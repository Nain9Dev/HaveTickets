const HOLDER_STORAGE_KEY = 'havetickets_holder_id';

function generateUuid(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export function getOrCreateHolderId(): string {
  let id = localStorage.getItem(HOLDER_STORAGE_KEY);
  if (!id || id.length !== 36) {
    id = generateUuid();
    localStorage.setItem(HOLDER_STORAGE_KEY, id);
  }
  return id;
}

export function resetHolderId(): string {
  const newId = generateUuid();
  localStorage.setItem(HOLDER_STORAGE_KEY, newId);
  return newId;
}
