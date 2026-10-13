## 3. Dependency direction

```text
Api -> Application -> Domain
                  -> ports/interfaces
Infrastructure -------^ implements ports
Integration.Axpert ---^ implements Axpert ports
Peg ------------------^ exposes workflow application contract
Workers -> Application/Infrastructure ports
```

Domain and Application projects must not depend directly on Redis, database providers, queue SDKs, or existing Axpert concrete classes.
