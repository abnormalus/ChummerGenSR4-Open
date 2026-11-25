import {fingerprint} from './core.mjs?v=f0c22eb4658216f295bf3dba670c3788820008d1';
// The working-copy bytes and remote hash form a durable outbox, including across reloads.
export class AutoSync {
 constructor({client,flush,list,acknowledge,validate,onAck,onConflict,report,online=()=>navigator.onLine}){Object.assign(this,{client,flush,list,acknowledge,validate,onAck,onConflict,report,online});this.running=null;}
 async exclusive(work){while(this.running)await this.running.catch(()=>{});const pending=Promise.resolve().then(work);this.running=pending;try{return await pending;}finally{if(this.running===pending)this.running=null;}}
 async tick(){
  if(this.running)return;const client=this.client();if(!client)return;
  if(!this.online()){this.report('Offline · Waiting to sync');return;}
  return this.exclusive(async()=>{
   let saved=0,errors=0,pending=0;
   try{
    await this.flush();const records=await this.list();
    for(const record of records){
     if(this.client()!==client)return;
     if((record.remote&&record.remote.repoId!==client.id)||(!record.remote&&record.pendingRepoId&&record.pendingRepoId!==client.id))continue;
     const hash=await fingerprint(record.bytes);if(record.remote?.hash===hash)continue;
     pending++;
     try{
      const bytes=this.validate(record.bytes),result=await client.sync(record,bytes);
      if(result.pull){errors++;this.onConflict(record,new Error('GitHub has a newer copy. Review before replacing local data.'));continue;}
      const latest=await this.acknowledge(record.id,result.remote);if(latest)this.onAck(latest);saved++;
     }catch(error){if(this.client()!==client)return;errors++;error.repoId=client.id;if(error.conflict||error.status===409)this.onConflict(record,error);else if(this.client()===client)this.report(error.status===401?'Reconnect GitHub · Edits saved locally':(error.status===403||error.status===429)?'GitHub access unavailable · Edits saved locally':'Waiting to sync · '+error.message,true);}
    }
    if(this.client()!==client)return;
    // Re-read after acknowledgements: edits made during a network request remain dirty.
    const latest=await this.list();let remaining=0;for(const r of latest){if((r.remote&&r.remote.repoId!==client.id)||(!r.remote&&r.pendingRepoId&&r.pendingRepoId!==client.id))continue;if(r.remote?.hash!==await fingerprint(r.bytes))remaining++;}
    if(!errors)this.report(remaining?`${remaining} character${remaining===1?'':'s'} waiting to sync`:`GitHub up to date · ${new Date().toLocaleTimeString([],{hour:'2-digit',minute:'2-digit'})}`);
   }catch(error){if(this.client()===client)this.report('Waiting to sync · '+error.message,true);}
  });
 }
}
