## 15. Files, AFL, and downloads

### 15.1 Input choices

- Small inline file payload: base64/byte representation subject to a strict limit.
- AFL reference: configured AFL name plus authorized file identity/path.
- Preferred for large files: separate streamed upload followed by an opaque file token in Set.

### 15.2 Security

- AFL credentials are resolved server-side from encrypted configuration/secret storage.
- Never serialize AFL credentials into AxData, logs, queues, or responses.
- Validate path traversal, allowed roots/buckets, file type, size, and malware policy.
- Store file tokens/references in AxData rather than long-lived binary bodies.
- Print/download artifacts use short-lived signed or authenticated access and an authorization check tied to tenant/ObjectId.
