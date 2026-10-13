using AxDataObject.Models.Dto;

namespace AxDataObject.Interface;

public interface IDataObjectService
{
    /// <summary>
    /// Sets a single field value in the user's DataObject.
    /// Triggers RefreshField for any dependent fields (Cexp/lookups).
    /// </summary>
    Task<SetValueResponse> SetFldValueAsync(SetValueRequest req);

    /// <summary>
    /// Reads a single field value from the user's DataObject.
    /// </summary>
    Task<GetValueResponse> GetFldValueAsync(GetValueRequest req);

    /// <summary>
    /// Batch-submits field values (e.g. on form load or bulk paste).
    /// Runs validation after setting each field.
    /// </summary>
    Task<SubmitValueResponse> SubmitValueAsync(SubmitValueRequest req);

    /// <summary>
    /// AxGet: loads record data from DB into the user's DataObject.
    /// Runs RefreshFieldsOnLoad (calculations, lookups) on all fields.
    /// </summary>
    Task<GetDataResponse> GetDataAsync(GetDataRequest req);

    /// <summary>
    /// AxPut: reads the user's DataObject from Redis, validates,
    /// generates SQL, and persists to DB in a transaction.
    /// </summary>
    //Task<SetDataResponse> SetDataAsync(SetDataRequest req);
}
