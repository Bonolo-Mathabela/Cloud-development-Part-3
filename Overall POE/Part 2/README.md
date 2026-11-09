<img width="2170" height="725" alt="image" src="https://github.com/user-attachments/assets/001961e4-198c-4449-8e09-9db05ed2bc83" />

- **Module:** CLDV6212 – POE Part 2
- **Student:** Bonolo Mathabela (ST10441596). Part 1 was done with Angel Maile (ST10455187).
- **Docker Hub image:** [bonolo05/coffeenchill-functions:v2.0](https://hub.docker.com/r/bonolo05/coffeenchill-functions)

## Contents

1. [What the app does](#1-what-the-app-does)
2. [How to run it](#2-how-to-run-it)
3. [Endpoints](#3-endpoints)
4. [How the order queue works](#4-how-the-order-queue-works)
5. [Testing with Postman](#5-testing-with-postman)
6. [Project structure](#6-project-structure)
7. [Problems and fixes](#7-problems-and-fixes)
8. [Changelog](#8-changelog)
9. [YouTube links](#9-youtube-links)
10. [References](#10-references)

---

## 1. What the app does

| Feature | Stored in |
|---|---|
| Menu – add, view, update and delete items (Part 1) | Table `MenuItems` |
| Staff documents – upload, list and download (Part 1) | Blob container `staff-docs` |
| Orders – placed on a queue and processed in the background (Part 2) | Queue `order-processing-queue`, Table `Orders` |

During break times many students order at once. Instead of processing each order while the student waits, the API puts the order on a queue and replies straight away. A queue-triggered function then saves the order and moves it through the stages:

```
Received -> Preparing -> Ready -> Collected   (30 seconds between each)
```

---

## 2. How to run it

You need Docker Desktop running. From the root of the repository:

```powershell
docker compose up
```

This pulls the app image from Docker Hub and starts two containers:

| Container | What it is | Port |
|---|---|---|
| `azurite` | Local Azure Storage emulator (blobs, queues, tables) | 10000-10002 |
| `coffeenchill-functions` | The Functions app, image `bonolo05/coffeenchill-functions:v2.0` | 7071 |

The compose file also creates a bridge network so the app can reach Azurite by name, a volume so the Azurite data is kept, and mounts the function keys from `docker/keys/host.json`.

Wait for `Host started` in the log. To check it works, run this in a second PowerShell window:

```powershell
Invoke-RestMethod http://localhost:7071/api/orders -Headers @{ "x-functions-key" = "CygywzCydtO1C0Y1SIK4Wf1aXAWErRJugFTYKhGC" }
```

Other useful commands:

```powershell
docker compose down      # stop the containers
docker compose down -v   # stop and delete the Azurite data
docker compose pull      # get the latest image from Docker Hub
```

**Running in Visual Studio instead:** open `CoffeeNChill/CoffeeNChill.slnx` and press F5. The app runs on `http://localhost:7053` and needs a `local.settings.json` with `"AzureWebJobsStorage": "UseDevelopmentStorage=true"`. Stop Docker Compose first, because Visual Studio starts its own Azurite on the same ports.

**Building the image:**

```powershell
cd CoffeeNChill
docker build -t bonolo05/coffeenchill-functions:v2.0 .
docker push bonolo05/coffeenchill-functions:v2.0
```

---

## 3. Endpoints

Base address: `http://localhost:7071`. Menu and order endpoints need the header `x-functions-key`. Document endpoints do not.

### Orders (Part 2)

| Method | Address | What it does |
|---|---|---|
| POST | `/api/orders/queue` | Validates the order and puts it on the queue. Returns 202, 400 or 503 |
| GET | `/api/orders/{orderId}` | Returns one order and its current status (404 if not processed yet) |
| GET | `/api/orders` | Returns all orders |

Example order (also in `CoffeeNChill/docs/sample-order.json`):

```json
{
  "OrderId": "ORD-2026-8801",
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-001", "PAS-104"],
  "TotalPrice": 65.00,
  "OrderTimestamp": "2026-10-12T10:15:00Z"
}
```

`CustomerName`, at least one SKU and a `TotalPrice` above 0 are required. If `OrderId` or `OrderTimestamp` is left out, the API generates them.

### Menu (Part 1)

| Method | Address | What it does |
|---|---|---|
| POST | `/api/menu` | Add a menu item |
| GET | `/api/menu` | Get all menu items |
| GET | `/api/menu/category/{category}` | Get items in one category |
| PUT | `/api/menu/{category}/{id}` | Update a menu item |
| DELETE | `/api/menu/{category}/{id}` | Delete a menu item |

### Staff documents (Part 1)

| Method | Address | What it does |
|---|---|---|
| POST | `/api/documents/upload?fileName={name}` | Upload a file (Postman: Body, binary) |
| GET | `/api/documents` | List all files |
| GET | `/api/documents/download/{fileName}` | Download a file |

---

## 4. How the order queue works

1. **QueueOrder** (`OrderFunctions.cs`) checks the order, serializes it to JSON and Base64 encodes it before sending it to `order-processing-queue`. Base64 is encoding, not encryption. It only turns the JSON into text that is safe to send as a queue message.
2. **ProcessOrderQueue** (`ProcessOrderQueue.cs`) has a `[QueueTrigger("order-processing-queue")]` attribute, so it runs as soon as a message arrives. It decodes the Base64 back to JSON and deserializes it into an `Order` object. `host.json` sets `messageEncoding` to `none` because the code does the encoding and decoding itself.
3. The order is saved to the `Orders` table with PartitionKey = order date, RowKey = OrderId and Status = `Received`.
4. The status is updated to `Preparing`, `Ready` and `Collected`, waiting 30 seconds between each. Each step is also written to the console log.
5. If a message fails 5 times, Azure Functions moves it to `order-processing-queue-poison`. **ProcessOrderQueuePoison** logs these messages so failed orders are not lost.

---

## 5. Testing with Postman

Import `CoffeNChill.postman_collection.json` and `CoffeNChill.local.postman_environment.json` from `CoffeeNChill/docs`, and select the **CoffeNChill Local** environment. Every request has sample data and tests.

Run the **Orders (Part 2)** folder in this order:

1. **Place Order** – 202 Accepted. Saves the OrderId for the next request.
2. **Get Order Status** – send it every 30 seconds to see Received, Preparing, Ready and Collected.
3. **Place Order - No OrderId** – 202, the API generates the id and timestamp.
4. **Place Order - Invalid** – 400, nothing is put on the queue.
5. **Get All Orders** – 200 with all orders.

The data can also be checked in Azure Storage Explorer under Emulator & Attached: **Tables > Orders** for the order rows and status, and **Queues > order-processing-queue** for the queue.

---

## 6. Project structure

```
cldv6212-g3-2026-poe-part2-group-22/
├── docker-compose.yml
├── docker/keys/host.json
├── README.md
└── CoffeeNChill/
    ├── Functions/
    │   ├── MenuFunctions.cs
    │   ├── DocumentFunctions.cs
    │   ├── OrderFunctions.cs
    │   └── ProcessOrderQueue.cs
    ├── Models/
    │   ├── MenuItem.cs
    │   └── Order.cs
    ├── docs/
    ├── Properties/
    ├── CoffeeNChill.csproj
    ├── CoffeeNChill.slnx
    ├── Dockerfile
    ├── host.json
    ├── Program.cs
    └── run-docker.ps1
```

---

## 7. Problems and fixes

| Problem | Fix |
|---|---|
| Container name "azurite" is already in use | Remove the Part 1 containers: `docker rm -f coffeenchill-functions azurite` |
| Port is already allocated | Stop Visual Studio or anything else using ports 7071 or 10000-10002 |
| 401 Unauthorized | Select the CoffeNChill Local environment in Postman and check the function key |
| Unable to connect | The app is still starting. Wait for `Host started` |
| Order status does not change | Each stage takes 30 seconds. Check `docker compose logs functions` |

---

## 8. Changelog

### v2.0 – Part 2

- Added `POST /api/orders/queue` with validation, OrderId and timestamp generation, JSON serialization, Base64 encoding and error handling.
- Added the `ProcessOrderQueue` queue-triggered function, which decodes the message, saves it to the `Orders` table and updates the status from Received to Collected.
- Added `ProcessOrderQueuePoison` to log failed messages.
- Added `GET /api/orders` and `GET /api/orders/{orderId}`.
- Added `Order.cs` and `sample-order.json`.
- Added `docker-compose.yml` using the Docker Hub image `v2.0`, a bridge network and a volume for Azurite.
- Added queue settings to `host.json` and the queue packages to the project.
- Updated the Dockerfile to .NET 10 to match the project.
- Added the Orders folder and tests to the Postman collection.
- Removed the unused `Function1.cs` template.

### v1.0 – Part 1

- Menu CRUD with Azure Table Storage.
- Staff document upload and download with Azure Blob Storage.
- Dockerfile, Postman collection and image `st10455187/coffeenchill-functions:v1.0`.

---

## 9. YouTube links

**Part 2:** 
- https://youtu.be/7EgsRVhwJ1U
**Part 1:**
- https://youtu.be/Hf01jgjKz9Y?si=td-0kRXkvq32rtBM
- https://youtu.be/zxpHqoXH1IA?si=iS3WtHNbRLaGph3w

---

## 10. References
Docker Inc. (n.d.) *Compose file reference*. Available at: https://docs.docker.com/reference/compose-file/ (Accessed: 12 October 2026).

Docker Inc. (n.d.) *Docker Compose overview*. Available at: https://docs.docker.com/compose/ (Accessed: 12 October 2026).

Microsoft (2025) *Use the Azurite emulator for local Azure Storage development*. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite (Accessed: 12 October 2026).

Microsoft (2026) *Azure Queue storage trigger for Azure Functions*. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger (Accessed: 12 October 2026).

Microsoft (n.d.) *Azure Queue Storage client library for .NET*. Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme (Accessed: 12 October 2026).

Microsoft (n.d.) *Azure Tables client library for .NET*. Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme (Accessed: 12 October 2026).

Microsoft (n.d.) *Convert.ToBase64String Method*. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.convert.tobase64string (Accessed: 12 October 2026).

Microsoft (n.d.) *Guide for running C# Azure Functions in an isolated worker process*. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide (Accessed: 12 October 2026).

Microsoft (n.d.) *How to write .NET objects as JSON (serialize)*. Available at: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to (Accessed: 12 October 2026).

Postman (n.d.) *Write scripts to test API response data in Postman*. Available at: https://learning.postman.com/docs/tests-and-scripts/write-scripts/test-scripts/ (Accessed: 12 October 2026).

---

