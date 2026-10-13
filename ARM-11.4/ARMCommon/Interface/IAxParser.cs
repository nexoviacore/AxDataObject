using System;
using System.Collections.Generic;

namespace ARMCommon.Interface
{
    public interface IAxParser
    {
        //List<string> VarList { get; set; }
        //List<string> ValueList { get; set; }
        //List<string> VarTypeList { get; set; }
        //string VarTypeStr { get; set; }
        //string ZeroValue { get; set; }
        //string LastVarType { get; set; }
        //string Value { get; set; }
        //List<string> ExpList { get; set; }
        //List<string> ExpTypeList { get; set; }
        //int ExpIndex { get; set; }
        //List<object> FncList { get; set; }
        //List<string> FunList { get; set; }
        //List<string> FunTypeList { get; set; }
        //List<string> RegVarFldList { get; set; }
        ////List<string> Parameters { get; set; }
        //string CallFunction(string functionname, string[] _params);
        //string MakeDate(string _dd, string _mon, string _yy);
        //int MonthOfDate(string dt);
        //int YearOfDate(string dt);
        //int DayOfDate(string dt);
        //int ReturnDMYOfDate(string dt, string src);
        //int IsValidDate(string dtStr);
        //string RegisterRegVariables(string varName, string varType, string varValue);
        //string CheckSpecialCharInGlobalVars(string input);
        //string RevCheckSpecialCharInGlobalVars(string input);
        //void RegisterVariables(string varName, string varType, string varValue);
        //string Compute(string value1, string value2, string op, string expressionType);
        //string EvaluateExpression(List<string> exprList, List<string> exprTypeList);
        //List<string> ConvertToPostFix(List<string> ExprList, List<string> ExprTypeList);
        //string EvaluatePostFix(List<string> ExprList, List<string> ExprTypeList);
        //string EvalFunction();
        //void EvalFun();
        //void CopyParam(string s);
        //bool Evaluate(string expr);
        //bool IsNumeral(string s);
        //void StartString();
        //void EndString();
        //Task<string> FireSql(string sqlName, string sqlText);
        //string ReplaceSqlParameters(string sql);
        //string SQLGet(string sqlName, string fieldName);
        //string SQLRegVar(string sqlText, string direct = "False");
        int RegisterVar(string varName, string varType, string value);
        //string GetVarValue(string varName);
        //string GetNumber(string input);
        //void EvalExprSet(int startpos);
        //void EvalExps(List<string> slist);
        //string GetNthString(string srcString, int strPos, char separator);
        //string GetNthString(string srcString, int strPos, string separator);
        //double StrToFloatZ(string s);
        //string GetValue(string pFieldName, string _rowNo);
        //double Total(string pFieldName);
    }
}