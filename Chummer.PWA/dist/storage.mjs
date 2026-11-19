const opened = new Promise((resolve, reject) => {
  const request = indexedDB.open(new URL(import.meta.url).searchParams.get('test') ? 'sr4-dossier-tests' : 'sr4-dossier:'+new URL('.',import.meta.url).pathname, 1);
  request.onupgradeneeded = () => { for (const name of ['characters', 'settings', 'backups']) request.result.createObjectStore(name); };
  request.onsuccess = () => resolve(request.result);
  request.onerror = () => reject(request.error);
});
async function transaction(stores, work) {
  const db = await opened;
  return new Promise((resolve, reject) => {
    const tx = db.transaction(stores, 'readwrite'); let result;
    tx.oncomplete = () => resolve(result); tx.onerror = tx.onabort = () => reject(tx.error || new Error('Local storage failed. Export a copy before closing.'));
    work(tx, value => { result = value; });
  });
}
export async function all() { const db = await opened; return new Promise((resolve,reject)=>{const r=db.transaction('characters').objectStore('characters').getAll();r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error);}); }
export const setting = async (key, text) => transaction(['settings'], (tx,done)=>{const s=tx.objectStore('settings');if(text !== undefined){s.put(text,key);done(text);}else{const r=s.get(key);r.onsuccess=()=>done(r.result);}});
export const save = record => transaction(['characters','backups'], (tx) => {
  const store=tx.objectStore('characters'), request=store.get(record.id);
  request.onsuccess=()=>{ if(request.result) tx.objectStore('backups').put(request.result,record.id); store.put(record,record.id); };
});
// Apply network acknowledgements to the latest working copy, never the uploaded snapshot.
export const acknowledge = (id, remote) => transaction(['characters'], (tx,done)=>{const store=tx.objectStore('characters'),r=store.get(id);r.onsuccess=()=>{if(!r.result)return;const latest={...r.result,remote};store.put(latest,id);done(latest);};});

export const remove = id => transaction(['characters','backups','settings'],tx=>{const store=tx.objectStore('characters'),r=store.get(id);r.onsuccess=()=>{if(r.result)tx.objectStore('backups').put(r.result,id);store.delete(id);};const selected=tx.objectStore('settings').get('selected');selected.onsuccess=()=>{if(selected.result===id)tx.objectStore('settings').put('','selected');};});
