# Struct pool
This repo contains a `FastPool<TObject>` class which can be used as a pool of structs. When going above current capacity the pool grows twice in size, but it doesn't release the memory. It's generally not very _safe_ but can be useful when doing some fast operations.

This code is a mental exercise I did for fun, so it's probably not "production ready" and can be further optimized. But it works and I'm very happy about it.