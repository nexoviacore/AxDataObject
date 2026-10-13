## 1. Scope

This document converts the AxData architecture into an implementable .NET design. `AxData-Architecture-Design.md` is the product and architectural source of truth. Names and example JSON in this document are proposed contracts until confirmed against the existing Axpert code and naming conventions.

The first implementation must be a new solution/project and must not modify existing StructDef, Validate, Parser, AxGet, AxPut, or PEG code without explicit review. Adapters isolate those dependencies.
