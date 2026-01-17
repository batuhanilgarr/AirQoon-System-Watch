// Mongo init for AirQoon
// Creates required indexes for airqoonBaseMapDB

const dbName = "airqoonBaseMapDB";
const dbRef = db.getSiblingDB(dbName);

try {
  dbRef.Tenants.createIndex({ SlugName: 1 }, { unique: true });
} catch (e) {
  // ignore
}

try {
  dbRef.Devices.createIndex({ TenantSlugName: 1 });
} catch (e) {
  // ignore
}

try {
  dbRef.Devices.createIndex({ DeviceId: 1 });
} catch (e) {
  // ignore
}

// Seed minimal sample data (idempotent)
// This allows the system to work out-of-the-box if you don't restore a real dump.
try {
  dbRef.Tenants.updateOne(
    { SlugName: "demo" },
    {
      $setOnInsert: {
        SlugName: "demo",
        Name: "Demo Tenant",
        IsPublic: true
      }
    },
    { upsert: true }
  );
} catch (e) {
  // ignore
}

try {
  dbRef.Devices.updateOne(
    { DeviceId: "demo-device-1" },
    {
      $setOnInsert: {
        DeviceId: "demo-device-1",
        TenantSlugName: "demo",
        Name: "Demo Device 1",
        Label: "Demo Device 1"
      }
    },
    { upsert: true }
  );
} catch (e) {
  // ignore
}
