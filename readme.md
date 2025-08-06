# infotecs-timescale

![.NET Build](https://github.com/portableclass/infotecs-timescale/actions/workflows/dotnet.yml/badge.svg)
[![codecov](https://codecov.io/github/portableclass/infotecs-timescale/branch/master/graph/badge.svg?token=LdIuai84ae)](https://codecov.io/github/portableclass/infotecs-timescale)
[![Docker Image](https://img.shields.io/docker/v/portableclass/timescaleapi?label=Docker%20Image&sort=semver)](https://hub.docker.com/r/portableclass/timescaleapi)


🔗 **Swagger UI:** [http://38.180.114.155:49152/swagger/](http://38.180.114.155:49152/swagger/)


-------



## 🚀 Implemented Endpoints

### 1. `POST /api/csv/upload`
Upload a CSV file with the format:
Date;ExecutionTime;Value
- Validates and saves raw/aggregated data to DB.
- Overwrites existing entries for the same file name.

📁 [Example CSV file](https://github.com/portableclass/infotecs-timescale/blob/master/example.csv)

### 2. `GET /api/results/filter`
Returns a filtered list of result entries with query parameters:
- `fileName`
- `dateFrom`, `dateTo`
- `avgExecutionTimeFrom`, `avgExecutionTimeTo`
- `avgValueFrom`, `avgValueTo`

### 3. `GET /api/results/last/{fileName}`
Returns the last 10 records of raw values for a given file name, sorted by ascending date.

--------

## 🛠️ Tech Stack

- **.NET 8**
- **EF Core**
- **PostgreSQL**
- **Swagger**
- **xUnit Unit Testing**
- **Docker-ready**

---------------

## repo on docker Hub

🛢 **portableclass/timescaleapi**  
→ [https://hub.docker.com/r/portableclass/timescaleapi](https://hub.docker.com/r/portableclass/timescaleapi)

```bash
docker pull portableclass/timescaleapi
