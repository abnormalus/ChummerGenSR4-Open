import {base64, unbase64, fingerprint, MAX_BYTES} from './core.mjs?v=9118072a45515f30414799bbdbbd68a7da265197';
export class GitHub {
  constructor(repository, token, request = (...args) => globalThis.fetch(...args)) {
    const match=repository.trim().replace(/^https:\/\/github.com\//,'').replace(/\/$/,'').match(/^([\w.-]+)\/([\w.-]+)$/);
    if(!match) throw new Error('Enter a GitHub repository as owner/repository.');
    this.repository=match[0];this.token=token;this.request=request;
  }
  async api(path, options={}) {
    const response=await this.request(`https://api.github.com/repos/${this.repository}${path}`,{...options,cache:'no-store',signal:options.signal||(typeof AbortSignal.timeout==='function'?AbortSignal.timeout(20000):undefined),headers:{Accept:'application/vnd.github+json',Authorization:`Bearer ${this.token}`,'X-GitHub-Api-Version':'2022-11-28',...options.headers}});
    if(!response.ok){const error=new Error(response.status===401?'GitHub session expired. Connect again.':response.status===409?'The repository changed. Reload its character before syncing.':`GitHub request failed (${response.status}). Your local copy is safe.`);error.status=response.status;throw error;}
    return response.json();
  }
  async verify() { const info=await this.api('');if(!info.private)throw new Error('Choose a private repository for character storage.');if(info.archived||!info.permissions?.push)throw new Error('This repository does not allow character saves.');this.id=info.id;this.branch=info.default_branch;return info; }
  path(path){return path.split('/').map(encodeURIComponent).join('/');}
  async list(){
    await this.verify();
    let tree;try{tree=await this.api('/git/trees/'+encodeURIComponent(this.branch)+'?recursive=1');}catch(error){if(error.status===404||error.status===409)return [];throw error;}
    let entries=tree.tree;
    if(!Array.isArray(entries))throw new Error('GitHub returned an unreadable character list.');
    if(tree.truncated){
      // Recurse individual trees if GitHub's recursive response reached its size limit.
      const walk=async(sha,prefix='')=>{const part=await this.api('/git/trees/'+encodeURIComponent(sha));if(part.truncated)throw new Error('GitHub could not return a complete folder listing.');const output=[];for(const entry of part.tree){const path=prefix+entry.path;if(entry.type==='tree')output.push(...await walk(entry.sha,path+'/'));else output.push({...entry,path});}return output;};
      entries=await walk(tree.sha);
    }
    return entries.filter(row=>row.type==='blob'&&row.mode!=='120000'&&/\.chum$/i.test(row.path)).map(row=>({...row,name:row.path.split('/').at(-1)})).sort((a,b)=>a.name.localeCompare(b.name)||a.path.localeCompare(b.path));
  }
  async readBlob(sha){const blob=await this.api('/git/blobs/'+encodeURIComponent(sha));if(blob.size>MAX_BYTES)throw new Error('Character exceeds 32 MB.');if(blob.encoding!=='base64')throw new Error('GitHub returned an unsupported character encoding.');return unbase64(blob.content);}
  async read(path){try{const file=await this.api('/contents/'+this.path(path));if(file.size>MAX_BYTES)throw new Error('Character exceeds 32 MB.');let data=file;if(file.encoding!=='base64')data=await this.api('/git/blobs/'+file.sha);return {sha:file.sha,bytes:unbase64(data.content)};}catch(e){if(e.status===404)return null;throw e;}}
  async sync(record, bytes){
    await this.verify(); const hash=await fingerprint(bytes),base=record.remote;
    if((base&&base.repoId!==this.id)||(!base&&record.pendingRepoId&&record.pendingRepoId!==this.id))throw new Error('This character is linked to a different repository. Import a separate working copy to use this repository.');
    const path=base?.path||record.pendingRemotePath||`characters/${record.id}.chum`,current=await this.read(path);
    if(base&&!current){const error=new Error('The GitHub character was removed. Your local copy is retained.');error.conflict=true;error.path=path;throw error;}
    if(current){
      const remoteHash=await fingerprint(current.bytes);
      if(remoteHash===hash)return {remote:{repoId:this.id,path,sha:current.sha,hash}};
      if(!base||current.sha!==base.sha){
        if(base&&hash===base.hash)return {pull:current.bytes,remote:{repoId:this.id,path,sha:current.sha,hash:remoteHash}};
        const error=new Error('Both copies changed. Download the repository character as a separate copy; your local edits have been retained.');error.conflict=true;error.path=path;throw error;
      }
    }
    // Recheck visibility immediately before writing. SHA is the conditional update guard.
    await this.verify();
    let result;try{result=await this.api('/contents/'+this.path(path),{method:'PUT',body:JSON.stringify({message:'Save character snapshot',content:base64(bytes),...(current?{sha:current.sha}:{})})});}catch(e){if(!e.status)throw new Error('Upload completion is uncertain. Sync again to check the remote snapshot; your local copy is safe.');throw e;}
    return {remote:{repoId:this.id,path,sha:result.content.sha,hash}};
  }
  async saveNew(path,bytes){
    await this.verify();if(await this.read(path))throw new Error('That GitHub filename already exists. Choose a different name.');await this.verify();
    const result=await this.api('/contents/'+this.path(path),{method:'PUT',body:JSON.stringify({message:'Save character under a new name',content:base64(bytes)})});
    return {repoId:this.id,path,sha:result.content.sha,hash:await fingerprint(bytes)};
  }
  async overwrite(path,bytes,expectedSha){
    if(expectedSha===undefined)throw new Error('Review the GitHub version before overwriting it.');
    await this.verify();const current=await this.read(path);
    if((current?.sha||null)!==expectedSha){const error=new Error('GitHub changed again. Review the conflict and choose again.');error.conflict=true;error.path=path;throw error;}
    await this.verify();const result=await this.api('/contents/'+this.path(path),{method:'PUT',body:JSON.stringify({message:'Resolve character conflict using local copy',content:base64(bytes),...(current?{sha:current.sha}:{})})});
    return {repoId:this.id,path,sha:result.content.sha,hash:await fingerprint(bytes)};
  }

}
