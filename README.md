# Glass Server

Server to provide simple access to MSFS 2020 via SimConnect.

# Building

- Copy the `{MSFS SDK}/SimConnect SDK` folder to the repo root.

# API

### Getting SimData
Request
```ts
// GET https://{HOST}/api/simdata/?name=PLANE%20LATITUDE&name=PLANE%20LONGITUDE
```

### Setting SimData
```ts
// POST https://{host}/api/simdata
// BODY
[ {"name": "LIGHT TAXI", "value": 0} ]
```

