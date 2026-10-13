## 6. Logical Architecture

```text
                    Client Applications / API Tools
                              |
                              v
                 API Gateway & Authentication
                              |
                              v
                    +--------------------+
                    |      AxData API    |
                    +--------------------+
                              |
                +-------------+-------------+
                |                           |
                v                           v
        AxData Processing             PEG Module
                |                           |
                |                    Authorization /
                |                    Workflow State
                |                           |
                |                    +----------------------+
                |                    | axactivetasks        |
                |                    | axactivetaskstatus   |
                |                    +----------------------+
                |                           |
                +-------------+-------------+
                              |
                              v
                 PEG Authorization Check
                              |
                 +------------+------------+
                 |                         |
              Allowed                    Denied
                 |                         |
                 v                         v
        Redis AxData Runtime State       Reject
              Store
                 |
                 v
        AxDataObjectInstance
            Runtime State
                 |
        +--------+---------+
        |                  |
        | ObjectId         |
        | TenantId         |
        | Creator/Initiator|
        | Current Approver |
        | PEG Task/Status  |
        | StructDef Version|
        | RecordId         |
        | Runtime Values   |
        | Dirty State      |
        | Lifecycle State  |
        +--------+---------+
                 |
                 v
       Axpert Transaction Database
             & Mappings
                 |
                 v
       Transactional Outbox | [Recommended - Push to queue directly | Avoid having outbox]
                 |
       +---------+-------------+----------------+
       |                       |                |
       v                       v                v
Persistence Worker       Print Worker      Notification /
                                            Integration Workers
                               |
                               v
                       Delphi Print Service
```

### Runtime Object Access

`AxDataObjectInstance` is maintained in Redis as shared runtime state rather than in the memory of a particular API server.

The `ObjectId` is used to identify the runtime object across API calls.

However, the `ObjectId` itself does not provide authorization.

Every request must first authenticate the user and validate the tenant context. The applicable PEG authorization must then determine whether the requesting user is permitted to access the AxData object.

Typical access can include:

```text
Creator / Initiator
        |
        +----> Access permitted

Current Approver
        |
        +----> Access permitted

Other User based on userpermission
        |
        +----> Access besed on user permission configuration

Other / Unauthorized User
        |
        +----> Access denied
```

Therefore, the logical request flow is:

```text
API Request
     |
     v
Authenticate User
     |
     v
Validate Tenant
     |
     v
Identify ObjectId
     |
     v
Validate PEG Authorization
     |
     +----> Unauthorized → Reject
     |
     v
Access AxDataObjectInstance
     |
     v
Redis Runtime State
     |
     v
Process / Validate / Modify
     |
     v
Persist to Transaction DB
     |
     v
Create Transactional Outbox Event
     |
     v
Background Workers
```

### Responsibility of Each Layer

| Component                            | Responsibility                                                                                     |
| ------------------------------------ | -------------------------------------------------------------------------------------------------- |
| **API Gateway / Authentication**     | Authenticate the caller and establish tenant/user/session context                                  |
| **AxData Module**                    | Manage AxData operations, validation, calculations, and transaction processing                     |
| **PEG Module**                       | Manage workflow/authorization and determine whether the user can access or act on an AxData object |
| **Redis Runtime State Store**        | Store active `AxDataObjectInstance` runtime state outside API server memory                        |
| **AxDataObjectInstance**             | Represent the current working state of an AxData operation                                         |
| **Axpert Transaction DB**            | Store persistent business/transaction data and mappings                                            |
| **Transactional Outbox**             | Reliably record events that need asynchronous processing                                           |
| **Persistence Worker**               | Process asynchronous persistence-related operations                                                |
| **Print Worker**                     | Handle print requests without blocking the API request                                             |
| **Notification/Integration Workers** | Process notifications, webhooks, and external integrations                                         |
| **Delphi Print Service**             | Execute the existing Delphi-based printing functionality                                           |

```

```
