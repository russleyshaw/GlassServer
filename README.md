# Glass Server

Server to provide simple access to MSFS 2020 via SimConnect.

# Building

- Copy the `{MSFS SDK}/SimConnect SDK` folder to the repo root.

# API

### Getting SimData
Request
```ts
// GET https://{host}/api/simdata
// BODY (application/json)
type Body = string[];
```

Result
```ts
// 200 OK
// BODY (application/json)
type Body = SimData[];
interface SimData {
  name: string;
  value?: number;
  units?: string;
}
```


