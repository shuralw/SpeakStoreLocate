// Simple IndexedDB wrapper for storing audio blobs offline
// Junior-friendly API:
// - AudioCache.save(blob): Speichert ein Audio offline.
// - AudioCache.listAll(): Liefert alle gespeicherten Audios (id, blob, timestamp).
// - AudioCache.remove(id): Entfernt ein gespeichertes Audio nach erfolgreichem Upload.
// Optional/fortgeschritten:
// - AudioCache.uploadAll(uploadFn): Lädt alle Einträge hoch (wird hier nicht mehr direkt genutzt).

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

  // Fetch all cached entries without modifying them
  // Prefer listAll() for clarity. getAll() is kept as alias.
  static async getAll(): Promise<Array<{ id: number; blob: Blob; timestamp: number }>> {
    return this.listAll();
  }

  // Junior-friendly name: listAll returns an array of { id, blob, timestamp }
  static async listAll(): Promise<Array<{ id: number; blob: Blob; timestamp: number }>> {
    const db = await this.open();
    try {
      const tx = db.transaction(this.storeName, 'readonly');
      const store = tx.objectStore(this.storeName);
      const all: Array<{ id: number; blob: Blob; timestamp: number }> = await new Promise((resolve, reject) => {
        const req = (store as any).getAll ? (store as any).getAll() : store.openCursor();
        if ((store as any).getAll) {
          req.onsuccess = () => resolve(req.result as any);
          req.onerror = () => reject(req.error);
        } else {
          const results: any[] = [];
          req.onsuccess = (event: any) => {
            const cursor = event.target.result;
            if (cursor) {
              results.push(cursor.value);
              cursor.continue();
            } else {
              resolve(results);
            }
          };
          req.onerror = () => reject(req.error);
        }
      });
      return all;
    } finally {
      db.close();
    }
  }

  // Remove a cached entry by its primary key id
  static async remove(id: number): Promise<void> {
    const db = await this.open();
    try {
      const tx = db.transaction(this.storeName, 'readwrite');
      const store = tx.objectStore(this.storeName);
      await new Promise<void>((resolve, reject) => {
        const req = store.delete(id);
        req.onsuccess = () => resolve();
        req.onerror = () => reject(req.error);
      });
    } finally {
      db.close();
    }
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
