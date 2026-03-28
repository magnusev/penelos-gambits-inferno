# Step 03 — Engine: Project Setup + Dependency

## What to do

1. Add `ktor-client-websockets` to `server/gradle/libs.versions.toml`:
   ```toml
   # Under [libraries]
   ktor-client-websockets = { module = "io.ktor:ktor-client-websockets", version.ref = "ktor" }
   ```

2. Add it to the `ktor-client` bundle (or directly to service deps):
   ```toml
   # Under [bundles] -> ktor-client, add:
   "ktor-client-websockets"
   ```

3. In `server/backend/penelos-gambits-service/build.gradle.kts`:
   - Verify the dependency is pulled in (via bundle or explicit `implementation`).

4. Update the placeholder `mainClass` in the same `build.gradle.kts`:
   ```kotlin
   ktorService {
       mainClass = "com.penelosgambits.service.ApplicationKt"
       // ...
   }
   ```

5. Create the source directory structure:
   ```
   server/backend/penelos-gambits-service/src/main/kotlin/com/penelosgambits/
   +-- service/
       +-- Application.kt   (just a main fun that logs "starting" for now)
   ```

6. Create a minimal `Application.kt`:
   ```kotlin
   package com.penelosgambits.service

   fun main() {
       println("Penelos Gambits Engine starting...")
   }
   ```

## Files to change / create

- `server/gradle/libs.versions.toml`
- `server/backend/penelos-gambits-service/build.gradle.kts`
- `server/backend/penelos-gambits-service/src/main/kotlin/com/penelosgambits/service/Application.kt` (new)

## How to verify

```bash
cd server && ./gradlew :backend:penelos-gambits-service:run
```

Should print "Penelos Gambits Engine starting..." and exit cleanly.
