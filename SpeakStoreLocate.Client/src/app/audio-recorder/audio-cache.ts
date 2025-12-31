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

  /**
   * Save a blob for later upload.
   * Persisted shape is backward-compatible with older entries.
   */
  static async save(blob: Blob, meta?: { status?: 'pending' | 'archived'; uploadedAt?: number; backendResults?: string[] }) {
    console.log('[AudioCache.save] Attempting to save blob:', blob);
    const db = await this.open();
    const tx = db.transaction(this.storeName, 'readwrite');
    const store = tx.objectStore(this.storeName);
    await new Promise<void>((resolve, reject) => {
      const req = store.add({
        blob,
        timestamp: Date.now(),
        status: meta?.status ?? 'pending',
        uploadedAt: meta?.uploadedAt,
        backendResults: meta?.backendResults,
      });
      req.onsuccess = () => {
        console.info('[AudioCache.save] Blob saved successfully.');
        resolve();
      };
      req.onerror = () => {
        console.error('[AudioCache.save] Error saving blob:', req.error);
        reject(req.error);
      };
    });
    tx.oncomplete = () => {
      console.log('[AudioCache.save] Transaction complete, closing DB.');
      db.close();
    };
    tx.onerror = () => {
      console.error('[AudioCache.save] Transaction error, closing DB.');
      db.close();
    };
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
  static async getAll(): Promise<Array<{ id: number; blob: Blob; timestamp: number; status?: 'pending' | 'archived'; uploadedAt?: number; backendResults?: string[] }>> {
    return this.listAll();
  }

  // Junior-friendly name: listAll returns an array of { id, blob, timestamp }
  static async listAll(): Promise<Array<{ id: number; blob: Blob; timestamp: number; status?: 'pending' | 'archived'; uploadedAt?: number; backendResults?: string[] }>> {
    const db = await this.open();
    try {
      const tx = db.transaction(this.storeName, 'readonly');
      const store = tx.objectStore(this.storeName);
      const all: Array<{ id: number; blob: Blob; timestamp: number; status?: 'pending' | 'archived'; uploadedAt?: number; backendResults?: string[] }> = await new Promise((resolve, reject) => {
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

  /** Mark a cached entry as archived (keeps the blob for future reprocessing). */
  static async markArchived(id: number, meta?: { uploadedAt?: number; backendResults?: string[] }): Promise<void> {
    const db = await this.open();
    try {
      const tx = db.transaction(this.storeName, 'readwrite');
      const store = tx.objectStore(this.storeName);

      const existing = await new Promise<any>((resolve, reject) => {
        const req = store.get(id);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
      });

      if (!existing) {
        console.warn('[AudioCache.markArchived] Entry not found:', id);
        return;
      }

      const updated = {
        ...existing,
        status: 'archived',
        uploadedAt: meta?.uploadedAt ?? Date.now(),
        backendResults: meta?.backendResults ?? existing.backendResults,
      };

      await new Promise<void>((resolve, reject) => {
        const req = store.put(updated);
        req.onsuccess = () => resolve();
        req.onerror = () => reject(req.error);
      });
      console.info('[AudioCache.markArchived] Archived entry:', id);
    } finally {
      db.close();
    }
  }

  /** Mark a cached entry as pending again (used for re-queueing archived uploads). */
  static async markPending(id: number): Promise<void> {
    const db = await this.open();
    try {
      const tx = db.transaction(this.storeName, 'readwrite');
      const store = tx.objectStore(this.storeName);

      const existing = await new Promise<any>((resolve, reject) => {
        const req = store.get(id);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
      });

      if (!existing) {
        console.warn('[AudioCache.markPending] Entry not found:', id);
        return;
      }

      const updated = {
        ...existing,
        status: 'pending',
        uploadedAt: undefined,
        backendResults: undefined,
      };

      await new Promise<void>((resolve, reject) => {
        const req = store.put(updated);
        req.onsuccess = () => resolve();
        req.onerror = () => reject(req.error);
      });
      console.info('[AudioCache.markPending] Marked pending entry:', id);
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
