using System;
using System.Collections;
using System.Collections.Generic;

namespace AxStructDef.Models
{
    public class TStringList: IEnumerable<string>
    {
        private readonly List<string> _items = new List<string>();

        public bool Sorted { get; set; } = false;

        public int Count => _items.Count;

        public string this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
                if (Sorted) Sort();
            }
        }

        public void Add(string value)
        {
            _items.Add(value);
            if (Sorted) Sort();
        }

         public int IndexOfName(string name)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var line = _items[i];
                var eqIndex = line.IndexOf('=');
                if (eqIndex >= 0)
                {
                    var key = line.Substring(0, eqIndex);
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public void Delete(int index) => _items.RemoveAt(index);

        public void Clear() => _items.Clear();

        // === Delphi-like functions ===

        // Get the name part (left side of '=')
        public string Names(int index)
        {
            var line = _items[index];
            var eqIndex = line.IndexOf('=');
            return eqIndex >= 0 ? line.Substring(0, eqIndex) : line;
        }

        // Alias for Delphi-style GetName
        public string GetName(int index) => Names(index);

        // Get the value part (right side of '=')
        public string ValueFromIndex(int index)
        {
            var line = _items[index];
            var eqIndex = line.IndexOf('=');
            return eqIndex >= 0 ? line.Substring(eqIndex + 1) : string.Empty;
        }

        // Alias for clarity
        public string GetValueFromIndex(int index) => ValueFromIndex(index);

        // Get value by key
        public string Values(string name)
        {
            foreach (var line in _items)
            {
                var eqIndex = line.IndexOf('=');
                if (eqIndex >= 0)
                {
                    var key = line.Substring(0, eqIndex);
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(eqIndex + 1);
                }
            }
            return string.Empty;
        }

        public void SetValue(string name, string value)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var eqIndex = _items[i].IndexOf('=');
                if (eqIndex >= 0)
                {
                    var key = _items[i].Substring(0, eqIndex);
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        _items[i] = $"{name}={value}";
                        if (Sorted) Sort();
                        return;
                    }
                }
            }
            _items.Add($"{name}={value}");
            if (Sorted) Sort();
        }

        private void Sort() => _items.Sort();

        public IEnumerator<string> GetEnumerator()
        {
            return _items.GetEnumerator(); 
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
