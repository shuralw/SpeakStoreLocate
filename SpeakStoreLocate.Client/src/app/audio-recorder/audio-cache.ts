// Simple IndexedDB wrapper for storing audio blobs offline
// Usage: await AudioCache.save(blob); await AudioCache.uploadAll(uploadFn);

export class AudioCache {
  private static dbName = 'audio-cache-db';
  private static storeName = 'audio-blobs';

  static async save(blob: Blob) {
    const db = await this.open();
    const tx = db.transaction(this.storeName, 'readwrite');
    const store = tx.objectStore(this.storeName);
    await new Promise<void>((resolve, reject) => {
      const req = store.add({ blob, timestamp: Date.now() });
      req.onsuccess = () => resolve();
      req.onerror = () => reject(req.error);
    });
    tx.oncomplete = () => db.close();
    tx.onerror = () => db.close();
  }

  static async uploadAll(uploadFn: (blob: Blob) => Promise<void>) {
    const db = await this.open();
    const tx = db.transaction(this.storeName, 'readwrite');
    const store = tx.objectStore(this.storeName);
    await new Promise<void>((resolve, reject) => {
      const cursorReq = store.openCursor();
      cursorReq.onsuccess = async (event: any) => {
        const cursor = event.target.result;
        if (cursor) {
          const entry = cursor.value;
          try {
            await uploadFn(entry.blob);
            cursor.delete();
            cursor.continue();
          } catch {
            // Stop on first failure to retry later
            resolve();
            return;
          }
        } else {
          resolve();
        }
      };
      cursorReq.onerror = () => reject(cursorReq.error);
    });
    tx.oncomplete = () => db.close();
    tx.onerror = () => db.close();
  }

  private static open(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(this.dbName, 1);
      req.onupgradeneeded = () => {
        req.result.createObjectStore(this.storeName, { keyPath: 'id', autoIncrement: true });
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }
}
