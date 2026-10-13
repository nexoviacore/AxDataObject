## 2. Existing Axpert capabilities to reuse

The new project will integrate with existing Axpert code without modifying it initially:

- `StructDef`: tstruct/form definition, including data containers, fields, FillGrid, GenMap, MdMap, table mappings, permissions, and related metadata.
- `Validate`: field validation, dependent-field evaluation, and refresh behavior.
- `Parser`: variables, expression evaluation, and calculations.
- Existing AxGet/AxPut APIs or their underlying logic: current database read/write behavior.
- Existing PEG routines and the `axactivetasks` and `axactivetaskstatus` tables.
- Existing Delphi print-form service, until it is replaced by a .NET implementation.

The integration will be through adapter interfaces. If an existing component cannot provide an operation needed by AxData, the limitation will be documented before any modification to existing code is proposed.
