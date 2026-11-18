import {base64, unbase64, fingerprint, MAX_BYTES} from './core.mjs';
export class GitHub {
  constructor(repository, token, request = (...args) => globalThis.fetch(...args)) {
    const match=repository.trim().replace(/^https:\/\/github.com\//,'').replace(/\/$/,'').match(/^([\w.-]+)\/([\w.-]+)$/);
    if(!match) throw new Error('Enter a GitHub repository as owner/repository.');
    this.repository=match[0];this.token=token;this.request=request;
  }
  async api(path, options={}) {
    const response=await this.request(`https://api.github.com/repos/${this.repository}${path}`,{...options,cache:'no-store',headers:{Accept:'application/vnd.github+json',Authorization:`Bearer ${this.token}`,'X-GitHub-Api-Version':'2022-11-28',...options.headers}});
    if(!response.ok){const error=new Error(response.status===401?'GitHub session expired. Connect again.':response.status===409?'The repository changed. Reload its character before syncing.':`GitHub request failed (${response.status}). Your local copy is safe.`);error.status=response.status;throw error;}
    return response.json();
  }
  async verify() { const info=await this.api('');if(!info.private)throw new Error('Choose a private repository for character storage.');if(info.archived||!info.permissions?.push)throw new Error('This repository does not allow character saves.');this.id=info.id;this.branch=info.default_branch;return info; }
  path(path){return path.split('/').map(encodeURIComponent).join('/');}
  async list(){await this.verify();try{const rows=await this.api('/contents/characters');if(!Array.isArray(rows))throw new Error('characters must be a folder.');return rows.filter(row=>row.type==='file'&&/\.chum$/i.test(row.name));}catch(e){if(e.status===404)return [];throw e;}}
  async read(path){try{const file=await this.api('/contents/'+this.path(path));if(file.size>MAX_BYTES)throw new Error('Character exceeds 32 MB.');let data=file;if(file.encoding!=='base64')data=await this.api('/git/blobs/'+file.sha);return {sha:file.sha,bytes:unbase64(data.content)};}catch(e){if(e.status===404)return null;throw e;}}
  async sync(record, bytes){
    await this.verify(); const hash=await fingerprint(bytes),base=record.remote;
    if(base&&base.repoId!==this.id)throw new Error('This character is linked to a different repository. Import a separate working copy to use this repository.');
    const path=base?.path||`characters/${record.id}.chum`,current=await this.read(path);
    if(base&&!current)throw new Error('The remote character was removed. Export your local copy before choosing a new location.');
    if(current){
      const remoteHash=await fingerprint(current.bytes);
      if(remoteHash===hash)return {remote:{repoId:this.id,path,sha:current.sha,hash}};
      if(!base||current.sha!==base.sha){
        if(base&&hash===base.hash)return {pull:current.bytes,remote:{repoId:this.id,path,sha:current.sha,hash:remoteHash}};
        const error=new Error('Both copies changed. Download the repository character as a separate copy; your local edits have been retained.');error.conflict=true;throw error;
      }
    }
    // Recheck visibility immediately before writing. SHA is the conditional update guard.
    await this.verify();
    let result;try{result=await this.api('/contents/'+this.path(path),{method:'PUT',body:JSON.stringify({message:'Save character snapshot',content:base64(bytes),...(current?{sha:current.sha}:{})})});}catch(e){if(!e.status)throw new Error('Upload completion is uncertain. Sync again to check the remote snapshot; your local copy is safe.');throw e;}
    return {remote:{repoId:this.id,path,sha:result.content.sha,hash}};
  }
}
