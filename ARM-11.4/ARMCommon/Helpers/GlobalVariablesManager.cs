
using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Xml;


namespace ARMCommon.Helpers
{
    public class GlobalVariablesManager
    {
        private List<string> _globalVars = new List<string>();
        private List<string> _globalVarsTypes = new List<string>();
        private List<string> _globalVarsValues = new List<string>();

        public Dictionary<string, string> GetGlobalVars()
        {
            var result = new Dictionary<string, string>();

            for (int i = 0; i < _globalVars.Count; i++)
            {
                result[_globalVars[i]] = _globalVarsValues[i];
            }

            return result;
        }

        public string GetGlobalVarsType()
        {
            return string.Join("", _globalVarsTypes);
        }

        public void AddOrUpdateGlobalVarToList(string varName, string vartype, string value)
        {
            if (string.IsNullOrEmpty(varName))
                throw new ArgumentException("Variable name cannot be null or empty", nameof(varName));

            int index = _globalVars.FindIndex(x => string.Equals(x, varName, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                // Update existing variable
                _globalVarsTypes[index] = vartype ?? string.Empty;
                _globalVarsValues[index] = value ?? string.Empty;
            }
            else
            {
                // Add new variable
                _globalVars.Add(varName);
                _globalVarsTypes.Add(vartype ?? string.Empty);
                _globalVarsValues.Add(value ?? string.Empty);
            }
        }


        public void AddOrUpdateGlobalVarsToList(List<string> varNames, List<string> vartypes, List<string> values)
        {
            if (varNames == null || vartypes == null || values == null)
                throw new ArgumentNullException("All parameter lists cannot be null");

            if (varNames.Count != vartypes.Count || varNames.Count != values.Count)
                throw new ArgumentException("All lists must have the same count");

            for (int i = 0; i < varNames.Count; i++)
            {
                AddOrUpdateGlobalVarToList(varNames[i], vartypes[i], values[i]);
            }
        }

        public bool RemoveGlobalVarFromList(string varName)
        {
            if (string.IsNullOrEmpty(varName))
                return false;

            int index = _globalVars.IndexOf(varName);
            if (index >= 0)
            {
                _globalVars.RemoveAt(index);
                _globalVarsTypes.RemoveAt(index);
                _globalVarsValues.RemoveAt(index);
                return true;
            }

            return false;
        }

        public int RemoveGlobalVarsFromList(List<string> varNames)
        {
            if (varNames == null)
                return 0;

            int removedCount = 0;

            foreach (var varName in varNames)
            {
                if (RemoveGlobalVarFromList(varName))
                    removedCount++;
            }

            return removedCount;
        }

        public void ClearGlobalVarsList()
        {
            _globalVars.Clear();
            _globalVarsTypes.Clear();
            _globalVarsValues.Clear();
        }

        // Additional utility methods
        public int GetGlobalVarsCount()
        {
            return _globalVars.Count;
        }

        public bool ContainsGlobalVar(string varName)
        {
            return _globalVars.Contains(varName);
        }

        public string GetGlobalVarType(string varName)
        {
            int index = _globalVars.IndexOf(varName);
            return index >= 0 ? _globalVarsTypes[index] : null;
        }

        public string GetGlobalVarValue(string varName)
        {
            int index = _globalVars.IndexOf(varName);
            return index >= 0 ? _globalVarsValues[index] : null;
        }

        // Get all variable information as a list of tuples
        public List<(string Name, string Type, string Value)> GetAllGlobalVarsInfo()
        {
            var result = new List<(string Name, string Type, string Value)>();

            for (int i = 0; i < _globalVars.Count; i++)
            {
                result.Add((_globalVars[i], _globalVarsTypes[i], _globalVarsValues[i]));
            }

            return result;
        }
    }
}
