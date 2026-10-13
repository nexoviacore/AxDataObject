using System;
using System.Data;
using System.Linq;

namespace AxStructDef.Wrappers;

/// <summary>
/// A C# wrapper class for a dataset, mimicking the functionality of a Delphi TClientDataSet.
/// This class provides in-memory data storage, filtering, and record management.
/// </summary>
public class TClientDataSetWrapper
{
    private DataTable _dataTable;
    private DataRow[] _filteredRows;
    private bool _isFiltered = false;
    private string _filterExpression;

    /// <summary>
    /// Gets the underlying DataTable.
    /// </summary>
    public DataTable DataSource
    {
        get { return _dataTable; }
    }

    /// <summary>
    /// Gets the number of records in the dataset.
    /// Returns the count of filtered records if the dataset is filtered.
    /// </summary>
    public int RecordCount
    {
        get
        {
            if (_isFiltered && _filteredRows != null)
            {
                return _filteredRows.Length;
            }
            return _dataTable?.Rows.Count ?? 0;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the dataset contains any records.
    /// </summary>
    public bool IsEmpty
    {
        get { return RecordCount == 0; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dataset is currently filtered.
    /// Setting this property to false clears the filter and makes all records visible.
    /// </summary>
    public bool Filtered
    {
        get { return _isFiltered; }
        set
        {
            if (value == false)
            {
                _isFiltered = false;
                _filteredRows = null;
            }
            else
            {
                // To enable filtering, a filter expression must be set first.
                // This is a simplified approach. In a real application, you might
                // enforce that Filter must be set before Filtered can be true.
            }
        }
    }

    /// <summary>
    /// Gets or sets the filter expression to apply to the dataset.
    /// The expression should be a valid string for DataTable.Select().
    /// Setting this property automatically applies the filter if Filtered is true.
    /// </summary>
    public string Filter
    {
        get { return _filterExpression; }
        set
        {
            _filterExpression = value;
            if (_isFiltered)
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Initializes the dataset with a new DataTable.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    public void CreateDataSet(string tableName)
    {
        _dataTable = new DataTable(tableName);
    }

    /// <summary>
    /// Adds a column to the dataset's schema.
    /// </summary>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="dataType">The data type of the column.</param>
    public void AddColumn(string columnName, Type dataType)
    {
        _dataTable.Columns.Add(columnName, dataType);
    }

    /// <summary>
    /// Adds a new row with provided data to the dataset.
    /// </summary>
    /// <param name="values">An array of values to populate the new row.</param>
    public void AddRow(params object[] values)
    {
        if (_dataTable == null)
        {
            throw new InvalidOperationException("DataTable is not created. Call CreateDataSet first.");
        }

        _dataTable.Rows.Add(values);
    }

    /// <summary>
    /// Executes the current filter expression on the data.
    /// </summary>
    private void ApplyFilter()
    {
        if (_dataTable != null && !string.IsNullOrEmpty(_filterExpression))
        {
            _filteredRows = _dataTable.Select(_filterExpression);
            _isFiltered = true;
        }
        else
        {
            _filteredRows = null;
            _isFiltered = false;
        }
    }

    /// <summary>
    /// Gets the value of a field in a record at the specified index.
    /// If the dataset is filtered, the index refers to the filtered set of records.
    /// </summary>
    /// <param name="index">The index of the record.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The value of the field.</returns>
    public object GetFieldValue(int index, string fieldName)
    {
        if (index < 0 || index >= RecordCount)
        {
            throw new IndexOutOfRangeException("Record index is out of bounds.");
        }

        DataRow row;
        if (_isFiltered)
        {
            row = _filteredRows[index];
        }
        else
        {
            row = _dataTable.Rows[index];
        }

        return row[fieldName];
    }
}