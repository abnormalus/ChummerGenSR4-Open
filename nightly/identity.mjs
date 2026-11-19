import {Character,fingerprint} from './core.mjs?v=cfc2c26877d4a48ffddd48ab18063abfbb00c564';
export const sameRemote=(a,b)=>!!a&&!!b&&a.repoId===b.repoId&&a.path===b.path;
export async function existingCharacter(records,{bytes,filename,remote,activeId}){
 const ordered=[...records].sort((a,b)=>Number(b.id===activeId)-Number(a.id===activeId)||(b.updatedAt||0)-(a.updatedAt||0));
 if(remote){const linked=ordered.find(r=>sameRemote(r.remote,remote));if(linked)return linked;}
 const hash=await fingerprint(bytes);
 for(const r of ordered){if(remote&&r.remote)continue;if(r.sourceHash===hash||r.remote?.hash===hash||await fingerprint(r.bytes)===hash)return r;}
 // Legacy local working copies lack an original hash. Require filename AND saved identity.
 if(!remote){const model=new Character(bytes);for(const r of ordered){if(r.sourceHash||r.remote||r.filename!==filename)continue;const saved=new Character(r.bytes);if(model.get('name')&&model.get('name')===saved.get('name')&&model.get('metatype')===saved.get('metatype'))return r;}}
 return null;
}
