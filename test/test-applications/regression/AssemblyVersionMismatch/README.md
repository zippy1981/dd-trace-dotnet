# Assembly version mismatch tests

These scripts will test an application which loads different versions of the .NET Tracer: one from the tracer home directory (like from an installer package) and another directly referenced by the app (like a NuGet package).

A "Pre-merge" tracer version contains separate assemblies for:
- Datadog.Trace
- Datadog.Trace.ClrProfiler.Managed
- Datadog.Trace.ClrProfiler.Managed.Core
- Datadog.Trace.AspNet

A "Post-merge" version refers to a tracer using the single `Datadog.Trace` assembly which includes all the code from the assemblies listed above. When comparing one post-merge version against another, we call them "post-merge" and "post-merge + 1".

## Dockerfiles

There are 10 Docker files, one for each test in the text matrix.
- The filename is `{runtime}-{tracer-home-version}-{nuget-version}.dockerfile`
- `{runtime}` can get `dotnet-framework` or `dotnet-core`
- The versions are coded as:
  - 1 = pre-merge
  - 2 = post-merge
  - 3 = post-merge + 1

## Test matrix

| Tracer Home  | NuGet          | Dockerfile                 |
| ------------ | -------------- | -------------------------- |
| pre-merge    | post-merge     | `{runtime}-1-2.dockerfile` |
| post-merge   | pre-merge      | `{runtime}-2-1.dockerfile` |
| post-merge   | (same version) | `{runtime}-2-2.dockerfile` |
| post-merge   | post-merge+1   | `{runtime}-2-3.dockerfile` |
| post-merge+1 | post-merge     | `{runtime}-3-2.dockerfile` |

## Steps

Determine versions:

| Name              | Version                 |
| ----------------- | ----------------------- |
| 1. Pre-merge      | 1.27.1                  |
| 2. Post-merge     | _current version_       |
| 3. Post-merge + 1 | _current version_ + 0.1 |

Prepare 3 tracer home directories:
- pre-merge: download and extract zip file `{pre-merge}` from github.com
  - https://github.com/DataDog/dd-trace-dotnet/releases/download/v1.27.1/windows-tracer-home.zip
- post-merge: build a `{post-merge}` tracer home directory
- post-merge+1: build a `{post-merge + 1}` tracer home directory

Prepare 3 nuget packages:
- pre-merge: download nuget package `{pre-merge}` from nuget.org
  - https://www.nuget.org/api/v2/package/Datadog.Trace/1.27.1
- post-merge: build a `{post-merge}` nuget package
- post-merge+1: build a `{post-merge + 1}` nuget package
