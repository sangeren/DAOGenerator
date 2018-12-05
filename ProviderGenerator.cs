namespace DAOGenerator
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Data.Entity.Design.PluralizationServices;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class ProviderGenerator : BaseGenerator, IGenerator
    {
        private string _outputPath;
        private string _namespaceName;
        private string _originalNamespace;
        private string _databaseName;
        private static List<Mapping> _mapping;
        private PluralizationService _pluralSvc;

        public ProviderGenerator(string outputPath, string namespaceName, string databaseName)
        {
            _outputPath = outputPath;
            _databaseName = databaseName;
            _namespaceName = string.Format("{0}.{1}.{2}", namespaceName, databaseName, "Provider");
            _originalNamespace = namespaceName;
            _mapping = MapperHelper.GetMappingInformation().ToList();
            _pluralSvc = PluralizationService.CreateService(new CultureInfo("en-us"));
        }

        public void Generate(Table table)
        {
            //grammar
            string tableName = MiscHelper.ReformatString(table.Name);
            string providerName = string.Empty;

            if (_pluralSvc.IsPlural(tableName))
            {
                tableName = _pluralSvc.Singularize(tableName);
            }

            providerName = tableName + "Provider";

            string fileName = string.Format("{0}\\{1}.{2}", _outputPath, providerName, _fileType);

            TextWriter tw = new StreamWriter(new FileStream(fileName, FileMode.Create));

            var codeProvider = CodeDomProvider.CreateProvider(_codeToGenerate);

            var ns = CodeGenHelper.CreateNamespace(_namespaceName);

            #region Imports

            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            ns.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
            ns.Imports.Add(new CodeNamespaceImport(string.Format("{0}.{1}.{2}", _originalNamespace, _databaseName, "Definition")));
            ns.Imports.Add(new CodeNamespaceImport(string.Format("{0}.{1}.{2}", _originalNamespace, _databaseName, "Ordinal")));
            ns.Imports.Add(new CodeNamespaceImport(string.Format("{0}.{1}.{2}", _originalNamespace, _databaseName, "Model")));
            ns.Imports.Add(new CodeNamespaceImport("MySql.Data.MySqlClient"));
            ns.Imports.Add(new CodeNamespaceImport("alfaruq.DbUtility"));

            #endregion

            var cls = CodeGenHelper.CreateClass(providerName, true);

            #region Create Provider Code here

            #region Constructor

            var cs = new CodeConstructor();
            cs.Attributes = MemberAttributes.Public;
            cs.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "connectionString"));
            cls.Members.Add(cs);
            cls.Members.Add(CodeGenHelper.CreateClassVariable(typeof(string), false, "_connectionString"));

            CodeAssignStatement as1 = new CodeAssignStatement(new CodeVariableReferenceExpression("_connectionString"),
                new CodeVariableReferenceExpression("connectionString"));

            cs.Statements.Add(as1);

            #endregion

            GenerateSelectAll(table, tableName, cls);

            GenerateSelect(table, tableName, cls);

            GenerateDelete(table, tableName, cls);

            GenerateInsert(table, tableName, cls);

            GenerateUpdate(table, tableName, cls);

            #endregion

            ns.Types.Add(cls);

            //generate the source code file
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";

            codeProvider.GenerateCodeFromNamespace(ns, tw, options);

            //close the text writer
            tw.Close();
        }

        private static void CoversionCodeAssignment(CodeIterationStatement whileDrRead, Schema schema)
        {
            var ca = new CodeAssignStatement();
            ca.Left = new CodeVariableReferenceExpression(string.Format("item.{0}", MiscHelper.ReformatString(schema.ColumnName)));
            string conversionMethod = string.Empty;

            switch (schema.DataType.ToLower())
            {
                case "int":
                    if ("YES".Equals(schema.Nullable.ToUpper()))
                    {
                        conversionMethod = "ConvertToIntNullable";
                    }
                    else
                    {
                        conversionMethod = "ConvertToInt";
                    }
                    break;
                case "varchar":
                    conversionMethod = "ConvertToString";
                    break;
                case "double":
                    if ("YES".Equals(schema.Nullable.ToUpper()))
                    {
                        conversionMethod = "ConvertToDoubleNullable";
                    }
                    else
                    {
                        conversionMethod = "ConvertToDouble";
                    }
                    break;
                case "datetime":
                    if ("YES".Equals(schema.Nullable.ToUpper()))
                    {
                        conversionMethod = "ConvertToDateTimeNullable";
                    }
                    else
                    {
                        conversionMethod = "ConvertToDateTime";
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(conversionMethod))
            {
                ca.Right = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Conversion"), conversionMethod,
                            new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("dr"),
                                new CodeVariableReferenceExpression(string.Format("Ordinal.{0}.{1}", schema.TableName, schema.ColumnName))));
                whileDrRead.Statements.Add(ca);
            }
        }

        private static void GenerateSelectAll(Table table, string tableName, CodeTypeDeclaration cls)
        {

            #region SelectAll Method

            #region SelectAll Parameters

            var csParam1 = new CodeParameterDeclarationExpression("System.String", "selectStatement");
            var codeAttr1 = new CodeAttributeArgument(new CodePrimitiveExpression(string.Format("SELECT * FROM {0};", table.Name)));
            csParam1.CustomAttributes = new CodeAttributeDeclarationCollection() { new CodeAttributeDeclaration("Optional", new CodeAttributeArgument[] { }),
                new CodeAttributeDeclaration("DefaultParameterValue", codeAttr1) };

            var csParam2 = new CodeParameterDeclarationExpression("List<Parameter>", "parameters");
            var codeAttr2 = new CodeAttributeArgument(new CodePrimitiveExpression(null));
            csParam2.CustomAttributes = new CodeAttributeDeclarationCollection() { new CodeAttributeDeclaration("Optional", new CodeAttributeArgument[] { }),
                new CodeAttributeDeclaration("DefaultParameterValue", codeAttr2) };

            #endregion

            CodeMemberMethod selectAll = new CodeMemberMethod();
            selectAll.Name = "SelectAll";
            selectAll.Parameters.Add(csParam1);
            selectAll.Parameters.Add(csParam2);
            selectAll.ReturnType = new CodeTypeReference(string.Format("IList<{0}>", tableName));
            selectAll.Attributes = MemberAttributes.Public;

            var result = new CodeVariableDeclarationStatement();
            result.Name = "Result";
            result.Type = new CodeTypeReference("var");
            result.InitExpression = new CodeObjectCreateExpression(string.Format("List<{0}>", tableName), new CodeExpression[] { });

            selectAll.Statements.Add(result);

            var factory = new CodeVariableDeclarationStatement();
            factory.Name = "factory";
            factory.Type = new CodeTypeReference("var");
            factory.InitExpression = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("MySqlClientFactory"), "Instance");

            selectAll.Statements.Add(factory);

            var connection = new CodeVariableDeclarationStatement();
            connection.Name = "con";
            connection.Type = new CodeTypeReference("var");
            connection.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("factory"), "CreateConnection");

            selectAll.Statements.Add(connection);

            var mainTry = new CodeTryCatchFinallyStatement();

            var conString = new CodeAssignStatement(new CodeVariableReferenceExpression("con.ConnectionString"), new CodeVariableReferenceExpression("_connectionString"));
            mainTry.TryStatements.Add(conString);

            var conOpen = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("con"), "Open", new CodeExpression[] { });
            mainTry.TryStatements.Add(conOpen);

            var cmd = new CodeVariableDeclarationStatement();
            cmd.Name = "cmd";
            cmd.Type = new CodeTypeReference("var");
            cmd.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("con"), "CreateCommand");

            mainTry.TryStatements.Add(cmd);

            var cmdCommandText = new CodeAssignStatement(new CodeVariableReferenceExpression("cmd.CommandText"), new CodeVariableReferenceExpression("selectStatement"));

            mainTry.TryStatements.Add(cmdCommandText);

            #region Process Parameters (Select Criteria)

            var checkParameters = new CodeConditionStatement();
            checkParameters.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("parameters"),
                CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));

            var loopParameters = new CodeIterationStatement();
            loopParameters.InitStatement = new CodeVariableDeclarationStatement(typeof(int), "i", new CodePrimitiveExpression(0));
            loopParameters.TestExpression = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.LessThan,
                new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("parameters"), "Count"));
            loopParameters.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("i"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1)));

            loopParameters.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "prm",
                new CodeObjectCreateExpression("MySqlParameter", new CodeExpression[] { })));

            var prmParameterName = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prm"), "ParameterName");
            var prmParameterNameValue = new CodeFieldReferenceExpression(new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("parameters"),
                new CodeVariableReferenceExpression("i")), "Name");

            loopParameters.Statements.Add(new CodeAssignStatement(prmParameterName, prmParameterNameValue));

            var prmParameterValue = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prm"), "Value");
            var prmParameterValueValue = new CodeFieldReferenceExpression(new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("parameters"),
                new CodeVariableReferenceExpression("i")), "Value");

            loopParameters.Statements.Add(new CodeAssignStatement(prmParameterValue, prmParameterValueValue));

            loopParameters.Statements.Add(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("cmd"), "Parameters"), "Add", new CodeVariableReferenceExpression("prm")));

            checkParameters.TrueStatements.Add(loopParameters);

            mainTry.TryStatements.Add(checkParameters);

            #endregion

            var dr = new CodeVariableDeclarationStatement();
            dr.Name = "dr";
            dr.Type = new CodeTypeReference("var");
            dr.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("cmd"), "ExecuteReader");

            mainTry.TryStatements.Add(dr);

            var whileDrRead = new CodeIterationStatement();
            whileDrRead.TestExpression = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("dr"), "Read", new CodeExpression[] { });
            whileDrRead.InitStatement = new CodeSnippetStatement("");
            whileDrRead.IncrementStatement = new CodeSnippetStatement("");

            var item = new CodeVariableDeclarationStatement();
            item.Name = "item";
            item.Type = new CodeTypeReference("var");
            item.InitExpression = new CodeObjectCreateExpression(tableName, new CodeExpression[] { });

            whileDrRead.Statements.Add(item);

            #region assign dr return value here

            foreach (var schema in table.Schemas.OrderBy(s => s.OrdinalPosition))
            {
                CoversionCodeAssignment(whileDrRead, schema);
            }

            #endregion

            var addItem = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Result"), "Add", new CodeVariableReferenceExpression("item"));

            whileDrRead.Statements.Add(addItem);

            mainTry.TryStatements.Add(whileDrRead);

            var catch1 = new CodeCatchClause("ex", new CodeTypeReference("Exception"));
            catch1.Statements.Add(new CodeThrowExceptionStatement(new CodeVariableReferenceExpression("ex")));
            mainTry.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("con"), new CodePrimitiveExpression(null)));

            mainTry.CatchClauses.Add(catch1);

            selectAll.Statements.Add(mainTry);
            selectAll.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("Result")));


            cls.Members.Add(selectAll);

            #endregion
        }



        private static void GenerateSelect(Table table, string tableName, CodeTypeDeclaration cls)
        {
            #region Select Method

            #region Select Parameters

            var primaryKeys = table.Schemas.FindAll(p => p.ColumnKey != null && p.ColumnKey.Equals("PRI"));

            if (primaryKeys.Count() > 1)
                throw new Exception(string.Format("Table {0} contains more than 1 primary key.", table.Name));

            var csParam1 = new CodeParameterDeclarationExpression("System.String", "selectStatement");
            var codeAttr1 = new CodeAttributeArgument(new CodePrimitiveExpression(string.Format("SELECT * FROM {0} WHERE {1} = @{2};",
                table.Name, primaryKeys.First().ColumnName, primaryKeys.First().ColumnName)));
            csParam1.CustomAttributes = new CodeAttributeDeclarationCollection() { new CodeAttributeDeclaration("Optional", new CodeAttributeArgument[] { }),
                new CodeAttributeDeclaration("DefaultParameterValue", codeAttr1) };

            var csParam2 = new CodeParameterDeclarationExpression("List<Parameter>", "parameters");
            var codeAttr2 = new CodeAttributeArgument(new CodePrimitiveExpression(null));
            csParam2.CustomAttributes = new CodeAttributeDeclarationCollection() { new CodeAttributeDeclaration("Optional", new CodeAttributeArgument[] { }),
                new CodeAttributeDeclaration("DefaultParameterValue", codeAttr2) };

            var csParam3 = new CodeParameterDeclarationExpression(_mapping.First(p => p.mySqlDataType.Equals(primaryKeys.First().DataType)).ClassName,
                MiscHelper.ReformatFieldString(primaryKeys.First().ColumnName));

            #endregion

            CodeMemberMethod select = new CodeMemberMethod();
            select.Name = "Select";
            select.Parameters.Add(csParam3);
            select.Parameters.Add(csParam1);
            select.Parameters.Add(csParam2);
            select.ReturnType = new CodeTypeReference(string.Format("{0}", tableName));
            select.Attributes = MemberAttributes.Public;

            var result = new CodeVariableDeclarationStatement();
            result.Name = "Result";
            result.Type = new CodeTypeReference("var");
            result.InitExpression = new CodeObjectCreateExpression(string.Format("{0}", tableName), new CodeExpression[] { });

            select.Statements.Add(result);

            var prm = new CodeVariableDeclarationStatement();
            prm.Name = "prms";
            prm.Type = new CodeTypeReference("var");
            prm.InitExpression = new CodeObjectCreateExpression("List<Parameter>", new CodeExpression[] { });

            select.Statements.Add(prm);

            var prmId = new CodeVariableDeclarationStatement();
            prmId.Name = "primaryKey";
            prmId.Type = new CodeTypeReference("var");
            prmId.InitExpression = new CodeObjectCreateExpression("Parameter", new CodeExpression[] { });

            select.Statements.Add(prmId);

            select.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("primaryKey"), "Name"),
                new CodePrimitiveExpression(string.Format("@{0}", primaryKeys.First().ColumnName))));
            select.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("primaryKey"), "Value"),
                new CodeVariableReferenceExpression(MiscHelper.ReformatFieldString(primaryKeys.First().ColumnName))));
            select.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("prms"), "Add", new CodeVariableReferenceExpression("primaryKey")));

            var factory = new CodeVariableDeclarationStatement();
            factory.Name = "factory";
            factory.Type = new CodeTypeReference("var");
            factory.InitExpression = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("MySqlClientFactory"), "Instance");

            select.Statements.Add(factory);

            var connection = new CodeVariableDeclarationStatement();
            connection.Name = "con";
            connection.Type = new CodeTypeReference("var");
            connection.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("factory"), "CreateConnection");

            select.Statements.Add(connection);

            var mainTry = new CodeTryCatchFinallyStatement();

            var conString = new CodeAssignStatement(new CodeVariableReferenceExpression("con.ConnectionString"), new CodeVariableReferenceExpression("_connectionString"));
            mainTry.TryStatements.Add(conString);

            var conOpen = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("con"), "Open", new CodeExpression[] { });
            mainTry.TryStatements.Add(conOpen);

            var cmd = new CodeVariableDeclarationStatement();
            cmd.Name = "cmd";
            cmd.Type = new CodeTypeReference("var");
            cmd.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("con"), "CreateCommand");

            mainTry.TryStatements.Add(cmd);

            var cmdCommandText = new CodeAssignStatement(new CodeVariableReferenceExpression("cmd.CommandText"), new CodeVariableReferenceExpression("selectStatement"));

            mainTry.TryStatements.Add(cmdCommandText);

            #region Process Parameters (Select Criteria)

            var checkParameters = new CodeConditionStatement();
            checkParameters.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("parameters"),
                CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));

            var addRange = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("prms"), "AddRange", new CodeVariableReferenceExpression("parameters"));

            checkParameters.TrueStatements.Add(addRange);

            mainTry.TryStatements.Add(checkParameters);

            var loopParameters = new CodeIterationStatement();
            loopParameters.InitStatement = new CodeVariableDeclarationStatement(typeof(int), "i", new CodePrimitiveExpression(0));
            loopParameters.TestExpression = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.LessThan,
                new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prms"), "Count"));
            loopParameters.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("i"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1)));

            loopParameters.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "prm",
                new CodeObjectCreateExpression("MySqlParameter", new CodeExpression[] { })));

            var prmParameterName = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prm"), "ParameterName");
            var prmParameterNameValue = new CodeFieldReferenceExpression(new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("prms"),
                new CodeVariableReferenceExpression("i")), "Name");

            loopParameters.Statements.Add(new CodeAssignStatement(prmParameterName, prmParameterNameValue));

            var prmParameterValue = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prm"), "Value");
            var prmParameterValueValue = new CodeFieldReferenceExpression(new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("prms"),
                new CodeVariableReferenceExpression("i")), "Value");

            loopParameters.Statements.Add(new CodeAssignStatement(prmParameterValue, prmParameterValueValue));

            loopParameters.Statements.Add(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("cmd"), "Parameters"), "Add", new CodeVariableReferenceExpression("prm")));

            mainTry.TryStatements.Add(loopParameters);

            #endregion

            var dr = new CodeVariableDeclarationStatement();
            dr.Name = "dr";
            dr.Type = new CodeTypeReference("var");
            dr.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("cmd"), "ExecuteReader");

            mainTry.TryStatements.Add(dr);

            var whileDrRead = new CodeIterationStatement();
            whileDrRead.TestExpression = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("dr"), "Read", new CodeExpression[] { });
            whileDrRead.InitStatement = new CodeSnippetStatement("");
            whileDrRead.IncrementStatement = new CodeSnippetStatement("");

            #region assign dr return value here

            foreach (var schema in table.Schemas.OrderBy(s => s.OrdinalPosition))
            {
                CoversionCodeAssignment(whileDrRead, schema);
            }

            #endregion            

            mainTry.TryStatements.Add(whileDrRead);

            var catch1 = new CodeCatchClause("ex", new CodeTypeReference("Exception"));
            catch1.Statements.Add(new CodeThrowExceptionStatement(new CodeVariableReferenceExpression("ex")));
            mainTry.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("con"), new CodePrimitiveExpression(null)));

            mainTry.CatchClauses.Add(catch1);

            select.Statements.Add(mainTry);
            select.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("Result")));

            cls.Members.Add(select);

            #endregion
        }

        private static void GenerateDelete(Table table, string tableName, CodeTypeDeclaration cls)
        {
            #region Parameters

            var primaryKeys = table.Schemas.FindAll(p => p.ColumnKey != null && p.ColumnKey.Equals("PRI"));

            if (primaryKeys.Count() > 1)
                throw new Exception(string.Format("Table {0} contains more than 1 primary key.", table.Name));

            var csParam1 = new CodeParameterDeclarationExpression(_mapping.First(p => p.mySqlDataType.Equals(primaryKeys.First().DataType)).ClassName,
                MiscHelper.ReformatFieldString(primaryKeys.First().ColumnName));

            #endregion

            CodeMemberMethod delete = new CodeMemberMethod();
            delete.Name = "Delete";
            delete.Parameters.Add(csParam1);
            delete.ReturnType = new CodeTypeReference(typeof(bool));
            delete.Attributes = MemberAttributes.Public;

            var result = new CodeVariableDeclarationStatement();
            result.Name = "Result";
            result.Type = new CodeTypeReference(typeof(bool));
            result.InitExpression = new CodePrimitiveExpression(false);

            delete.Statements.Add(result);

            var sqlStatement = new CodeVariableDeclarationStatement();
            sqlStatement.Name = "deleteStatement";
            sqlStatement.Type = new CodeTypeReference(typeof(string));
            sqlStatement.InitExpression = new CodePrimitiveExpression(string.Format("DELETE FROM {0} WHERE {1} = @{2}",
                table.Name, primaryKeys.First().ColumnName, primaryKeys.First().ColumnName));

            delete.Statements.Add(sqlStatement);

            var factory = new CodeVariableDeclarationStatement();
            factory.Name = "factory";
            factory.Type = new CodeTypeReference("var");
            factory.InitExpression = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("MySqlClientFactory"), "Instance");

            delete.Statements.Add(factory);

            var connection = new CodeVariableDeclarationStatement();
            connection.Name = "con";
            connection.Type = new CodeTypeReference("var");
            connection.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("factory"), "CreateConnection");

            delete.Statements.Add(connection);

            var mainTry = new CodeTryCatchFinallyStatement();

            var conString = new CodeAssignStatement(new CodeVariableReferenceExpression("con.ConnectionString"), new CodeVariableReferenceExpression("_connectionString"));
            mainTry.TryStatements.Add(conString);

            var conOpen = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("con"), "Open", new CodeExpression[] { });
            mainTry.TryStatements.Add(conOpen);

            var cmd = new CodeVariableDeclarationStatement();
            cmd.Name = "cmd";
            cmd.Type = new CodeTypeReference("var");
            cmd.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("con"), "CreateCommand");

            mainTry.TryStatements.Add(cmd);

            var cmdCommandText = new CodeAssignStatement(new CodeVariableReferenceExpression("cmd.CommandText"), new CodeVariableReferenceExpression("deleteStatement"));

            mainTry.TryStatements.Add(cmdCommandText);

            mainTry.TryStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "prm",
                new CodeObjectCreateExpression("MySqlParameter", new CodeExpression[] { })));

            var prmParameterName = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prm"), "ParameterName");
            var prmParameterNameValue = new CodePrimitiveExpression(string.Format("@{0}", primaryKeys.First().ColumnName));

            mainTry.TryStatements.Add(new CodeAssignStatement(prmParameterName, prmParameterNameValue));

            var prmParameterValue = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("prm"), "Value");
            var prmParameterValueValue = new CodeVariableReferenceExpression(MiscHelper.ReformatFieldString(primaryKeys.First().ColumnName));

            mainTry.TryStatements.Add(new CodeAssignStatement(prmParameterValue, prmParameterValueValue));

            mainTry.TryStatements.Add(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("cmd"), "Parameters"), "Add",
                new CodeVariableReferenceExpression("prm")));

            mainTry.TryStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "i",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("cmd"), "ExecuteNonQuery")));

            var checkParameters = new CodeConditionStatement();
            checkParameters.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.GreaterThan, new CodePrimitiveExpression(0));

            checkParameters.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Result"), new CodePrimitiveExpression(true)));

            mainTry.TryStatements.Add(checkParameters);

            var catch1 = new CodeCatchClause("ex", new CodeTypeReference("Exception"));
            catch1.Statements.Add(new CodeThrowExceptionStatement(new CodeVariableReferenceExpression("ex")));
            mainTry.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("con"), new CodePrimitiveExpression(null)));

            mainTry.CatchClauses.Add(catch1);

            delete.Statements.Add(mainTry);

            delete.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("Result")));

            cls.Members.Add(delete);
        }

        private static void GenerateInsert(Table table, string tableName, CodeTypeDeclaration cls)
        {
            #region Parameters

            var csParam1 = new CodeParameterDeclarationExpression(tableName, MiscHelper.ReformatFieldString(tableName));

            #endregion

            CodeMemberMethod insert = new CodeMemberMethod();
            insert.Name = "Insert";
            insert.Parameters.Add(csParam1);
            insert.ReturnType = new CodeTypeReference(typeof(bool));
            insert.Attributes = MemberAttributes.Public;

            var result = new CodeVariableDeclarationStatement();
            result.Name = "Result";
            result.Type = new CodeTypeReference(typeof(bool));
            result.InitExpression = new CodePrimitiveExpression(false);

            insert.Statements.Add(result);

            string cols = MiscHelper.GenerateColumnsSelection(table.Schemas.OrderBy(s => s.OrdinalPosition).Select(s => s.ColumnName).ToArray());
            string prms = MiscHelper.GenerateColumnsSelection(table.Schemas.OrderBy(s => s.OrdinalPosition).Select(s => s.ColumnName).ToArray(), true);

            string insertStatement = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", table.Name, cols, prms);

            insert.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(string)), "insertStatement", new CodePrimitiveExpression(insertStatement)));

            var factory = new CodeVariableDeclarationStatement();
            factory.Name = "factory";
            factory.Type = new CodeTypeReference("var");
            factory.InitExpression = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("MySqlClientFactory"), "Instance");

            insert.Statements.Add(factory);

            var connection = new CodeVariableDeclarationStatement();
            connection.Name = "con";
            connection.Type = new CodeTypeReference("var");
            connection.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("factory"), "CreateConnection");

            insert.Statements.Add(connection);

            var mainTry = new CodeTryCatchFinallyStatement();

            var conString = new CodeAssignStatement(new CodeVariableReferenceExpression("con.ConnectionString"), new CodeVariableReferenceExpression("_connectionString"));
            mainTry.TryStatements.Add(conString);

            var conOpen = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("con"), "Open", new CodeExpression[] { });
            mainTry.TryStatements.Add(conOpen);

            var cmd = new CodeVariableDeclarationStatement();
            cmd.Name = "cmd";
            cmd.Type = new CodeTypeReference("var");
            cmd.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("con"), "CreateCommand");

            mainTry.TryStatements.Add(cmd);

            var cmdCommandText = new CodeAssignStatement(new CodeVariableReferenceExpression("cmd.CommandText"), new CodeVariableReferenceExpression("insertStatement"));

            mainTry.TryStatements.Add(cmdCommandText);

            int counter = 0;
            foreach (var schema in table.Schemas)
            {
                string prmName = string.Format("prm{0}", counter);
                var prm = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), prmName, new CodeObjectCreateExpression("MySqlParameter", new CodeExpression[] { }));

                mainTry.TryStatements.Add(prm);

                var prmParameterName = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(prmName), "ParameterName");
                var prmParameterNameValue = new CodePrimitiveExpression(string.Format("@{0}", schema.ColumnName));

                mainTry.TryStatements.Add(new CodeAssignStatement(prmParameterName, prmParameterNameValue));

                var prmParameterValue = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(prmName), "Value");
                var prmParameterValueValue = new CodeVariableReferenceExpression(string.Format("{0}.{1}", MiscHelper.ReformatFieldString(tableName), MiscHelper.ReformatString(schema.ColumnName)));

                mainTry.TryStatements.Add(new CodeAssignStatement(prmParameterValue, prmParameterValueValue));

                mainTry.TryStatements.Add(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("cmd"), "Parameters"), "Add",
                new CodeVariableReferenceExpression(prmName)));

                counter++;
            }

            mainTry.TryStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "i",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("cmd"), "ExecuteNonQuery")));

            var checkParameters = new CodeConditionStatement();
            checkParameters.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.GreaterThan, new CodePrimitiveExpression(0));

            checkParameters.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Result"), new CodePrimitiveExpression(true)));

            mainTry.TryStatements.Add(checkParameters);

            var catch1 = new CodeCatchClause("ex", new CodeTypeReference("Exception"));
            catch1.Statements.Add(new CodeThrowExceptionStatement(new CodeVariableReferenceExpression("ex")));
            mainTry.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("con"), new CodePrimitiveExpression(null)));

            mainTry.CatchClauses.Add(catch1);

            insert.Statements.Add(mainTry);

            insert.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("Result")));

            cls.Members.Add(insert);
        }

        private static void GenerateUpdate(Table table, string tableName, CodeTypeDeclaration cls)
        {
            //update table set column1 = value1, column2 = value2 where crit1 = id

            #region Parameters

            var primaryKeys = table.Schemas.FindAll(p => p.ColumnKey != null && p.ColumnKey.Equals("PRI"));

            var csParam1 = new CodeParameterDeclarationExpression(tableName, MiscHelper.ReformatFieldString(tableName));

            #endregion

            CodeMemberMethod update = new CodeMemberMethod();
            update.Name = "Update";
            update.Parameters.Add(csParam1);
            update.ReturnType = new CodeTypeReference(typeof(bool));
            update.Attributes = MemberAttributes.Public;

            var result = new CodeVariableDeclarationStatement();
            result.Name = "Result";
            result.Type = new CodeTypeReference(typeof(bool));
            result.InitExpression = new CodePrimitiveExpression(false);

            update.Statements.Add(result);

            string updateTemplateStatement = "UPDATE {0} SET {1} WHERE {2} = @{2};";

            var setColumns = MiscHelper.GenerateColumnsUpdate(table.Schemas.Where(p => p.ColumnKey == null).OrderBy(s => s.OrdinalPosition).Select(s => s.ColumnName).ToArray());

            string updateStatement = string.Format(updateTemplateStatement, table.Name, setColumns, primaryKeys[0].ColumnName);

            update.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(string)), "updateStatement", new CodePrimitiveExpression(updateStatement)));

            var factory = new CodeVariableDeclarationStatement();
            factory.Name = "factory";
            factory.Type = new CodeTypeReference("var");
            factory.InitExpression = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("MySqlClientFactory"), "Instance");

            update.Statements.Add(factory);

            var connection = new CodeVariableDeclarationStatement();
            connection.Name = "con";
            connection.Type = new CodeTypeReference("var");
            connection.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("factory"), "CreateConnection");

            update.Statements.Add(connection);

            var mainTry = new CodeTryCatchFinallyStatement();

            var conString = new CodeAssignStatement(new CodeVariableReferenceExpression("con.ConnectionString"), new CodeVariableReferenceExpression("_connectionString"));
            mainTry.TryStatements.Add(conString);

            var conOpen = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("con"), "Open", new CodeExpression[] { });
            mainTry.TryStatements.Add(conOpen);

            var cmd = new CodeVariableDeclarationStatement();
            cmd.Name = "cmd";
            cmd.Type = new CodeTypeReference("var");
            cmd.InitExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("con"), "CreateCommand");

            mainTry.TryStatements.Add(cmd);

            var cmdCommandText = new CodeAssignStatement(new CodeVariableReferenceExpression("cmd.CommandText"), new CodeVariableReferenceExpression("updateStatement"));

            mainTry.TryStatements.Add(cmdCommandText);

            int counter = 0;
            foreach (var schema in table.Schemas)
            {
                string prmName = string.Format("prm{0}", counter);
                var prm = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), prmName, new CodeObjectCreateExpression("MySqlParameter", new CodeExpression[] { }));

                mainTry.TryStatements.Add(prm);

                var prmParameterName = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(prmName), "ParameterName");
                var prmParameterNameValue = new CodePrimitiveExpression(string.Format("@{0}", schema.ColumnName));

                mainTry.TryStatements.Add(new CodeAssignStatement(prmParameterName, prmParameterNameValue));

                var prmParameterValue = new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(prmName), "Value");
                var prmParameterValueValue = new CodeVariableReferenceExpression(string.Format("{0}.{1}", MiscHelper.ReformatFieldString(tableName), MiscHelper.ReformatString(schema.ColumnName)));

                mainTry.TryStatements.Add(new CodeAssignStatement(prmParameterValue, prmParameterValueValue));

                mainTry.TryStatements.Add(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("cmd"), "Parameters"), "Add",
                new CodeVariableReferenceExpression(prmName)));

                counter++;
            }

            mainTry.TryStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "i",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("cmd"), "ExecuteNonQuery")));

            var checkParameters = new CodeConditionStatement();
            checkParameters.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.GreaterThan, new CodePrimitiveExpression(0));

            checkParameters.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("Result"), new CodePrimitiveExpression(true)));

            mainTry.TryStatements.Add(checkParameters);

            var catch1 = new CodeCatchClause("ex", new CodeTypeReference("Exception"));
            catch1.Statements.Add(new CodeThrowExceptionStatement(new CodeVariableReferenceExpression("ex")));
            mainTry.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("con"), new CodePrimitiveExpression(null)));

            mainTry.CatchClauses.Add(catch1);

            update.Statements.Add(mainTry);

            update.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("Result")));

            cls.Members.Add(update);
        }

        public void Generate(List<Table> tables)
        {
            throw new NotImplementedException();
        }
    }
}