import {Character,demo,value,integer,truth,MAX_BYTES,fingerprint} from './core.mjs';
import * as db from './storage.mjs';
import {GitHub} from './github.mjs';
import {characterPreview,matchesCharacter} from './repository-browser.mjs';
import {installController} from './install.mjs';
import {existingCharacter,sameRemote} from './identity.mjs';
import {AutoSync} from './autosync.mjs';
import {filteredSkills,magicSkillsApply} from './skill-filters.mjs';
const $=id=>document.getElementById(id);
const el=(tag,text,cls)=>{const node=document.createElement(tag);if(text!==undefined)node.textContent=text;if(cls)node.className=cls;return node;};
let records=[],record,character,page='Character',positive=true,skillCategory='Active',client,remoteRows=[],saving=Promise.resolve(),timer;
let syncNote='',persistedModel,persistedRevision=0,currentConflict;
const conflicts=new Map(),pendingConflicts=new Set();
function status(text,error=false){$('status').textContent=text+(syncNote?' · '+syncNote:'');$('status').className=error?'error':'';if(conflicts.size)$('status').append(document.createTextNode(' '),button('Resolve conflict',()=>showConflict(conflicts.keys().next().value)));}

function run(fn){return async(...args)=>{try{await fn(...args);}catch(error){status(error.message,true);}};}
function button(text,fn){const b=el('button',text);b.type='button';b.onclick=run(fn);return b;}
function section(title,action){const s=el('section'),h=el('h2',title);if(action)h.append(action);s.append(h);$('main').append(s);return s;}
function row(parent,name,detail,source){const r=el('div',undefined,'row');r.append(el('span',name,'name'));if(detail instanceof Node)r.append(detail);else if(detail)r.append(el('span',detail));if(source)r.append(el('small',source));parent.append(r);return r;}
function source(node){return [value(node,'source'),value(node,'page')&&'p. '+value(node,'page')].filter(Boolean).join(' ');}
function input(text,change,label){const i=el('input');i.type='text';i.value=text;i.setAttribute('aria-label',label);i.oninput=()=>{change(i.value);schedule();};i.onchange=run(async()=>{await flush();});return i;}
function schedule(){syncNote=client?'GitHub: waiting to sync':'GitHub: connect to sync';status('Saving on this device…');clearTimeout(timer);timer=setTimeout(()=>run(flush)(),200);}
async function flush(){clearTimeout(timer);if(!record||!character)return;const model=character,revision=model.revision;const snapshot={...record,title:model.title,bytes:model.bytes(),updatedAt:Date.now()};if(revision>0&&(persistedModel!==model||persistedRevision!==revision))syncNote=client?'GitHub: waiting to sync':'GitHub: connect to sync';record=snapshot;const operation=saving.then(()=>db.save(snapshot));saving=operation.catch(()=>{});await operation;if(character===model){persistedModel=model;persistedRevision=revision;}records=records.map(r=>r.id===snapshot.id?snapshot:r);status('Saved on this device'+(navigator.onLine?'':' · Offline'));}
async function select(id){await flush();const next=records.find(r=>r.id===id);if(!next)return;character=new Character(next.bytes);record=next;await db.setting('selected',id);render();}
async function add(bytes,filename,remote){
 const model=new Character(bytes);await flush();
 const existing=await existingCharacter(records,{bytes,filename,remote,activeId:record?.id});
 if(existing){await select(existing.id);if(remote&&!record.remote){record={...record,remote};await db.save(record);records=records.map(r=>r.id===record.id?record:r);}status('Switched to saved local character · Edits retained');return;}
 const next={id:crypto.randomUUID(),filename,title:model.title,bytes,sourceHash:await fingerprint(bytes),updatedAt:Date.now(),...(remote?{remote}:{})};
 await db.save(next);records.push(next);record=next;character=model;await db.setting('selected',next.id);render();syncNote=client?'GitHub: waiting to sync':'GitHub: connect to sync';status('Imported · Saved on this device');navigator.storage?.persist?.();
}
const installer=installController(window,navigator,updateInstallButtons);
function updateInstallButtons(){for(const b of document.querySelectorAll('[data-install]')){b.textContent=installer.installed()?'Installed':'Install app';b.disabled=installer.installed();}}
async function installApp(){const result=await installer.request();if(result.kind==='guide'){$('install-steps').replaceChildren(...result.steps.map(step=>el('li',step)));$('install-help').showModal();}else status(result.kind==='installed'?'This app is already installed.':result.kind==='accepted'?'Installation accepted. Open SR 4 Dossier from your Home screen or desktop.':'Installation dismissed. You can try again from the start page.');}
let closing=false;
async function closeCharacter(){if(!character||closing)return;closing=true;$('main').inert=true;document.querySelector('header').inert=true;try{await flush();await db.setting('selected','');record=undefined;character=undefined;page='Character';render();status('Character closed · Changes saved on this device');void syncer.tick();}finally{closing=false;$('main').inert=false;document.querySelector('header').inert=false;}}
$('close-character').onclick=run(closeCharacter);
function render(){
  const picker=$('characters');picker.replaceChildren();if(!record){const placeholder=el('option','Choose a saved character…');placeholder.value='';placeholder.selected=true;picker.append(placeholder);}for(const r of records){const option=el('option',r.title);option.value=r.id;option.selected=r.id===record?.id;picker.append(option);}
  $('export').disabled=!character;$('close-character').hidden=!character;$('main').replaceChildren();const nav=document.querySelector('nav');nav.replaceChildren();
  if(!character){
   const box=el('div',undefined,'empty'),icon=el('img');icon.src='./assets/icon.png';icon.alt='';box.append(icon,el('h1','SR 4 Dossier'),el('p','Your runner, ready for the table. Open a saved character or import a Chummer file.'));
   const actions=el('div',undefined,'home-actions');const install=button('Install app',installApp);install.dataset.install='';actions.append(install,button('Open from GitHub',openRepository),button('Import .chum',()=>$('file').click()),button('Try sample',()=>add(demo,'Nightjar.chum')));box.append(actions);$('main').append(box);updateInstallButtons();
   if(records.length){const saved=section('Saved on this device');for(const r of [...records].sort((a,b)=>b.updatedAt-a.updatedAt)){const b=button('Resume '+r.title,()=>select(r.id));b.className='saved-character';saved.append(b);}}
   return;
  }
  for(const name of ['Character','Combat','Equipment','Background','Contacts']){const b=button(name,async()=>{await flush();page=name;render();window.scrollTo(0,0);});b.className=page===name?'active':'';nav.append(b);}
  if(page==='Character'){
    const identity=section('Dossier');for(const [key,label] of [['alias','Runner'],['name','Name'],['metatype','Metatype']]){const i=input(character.get(key),v=>character.set(key,v),label);i.className='wide';row(identity,label,i);}
    const stats=section('Attributes'),grid=el('div',undefined,'attributes');stats.append(grid);for(const a of character.attributes){const label=el('label',a.id);label.append(input(a.total,v=>character.setAttribute(a.id,'total',v),a.id+' total'));grid.append(label);}
    row(stats,'Essence',input(character.essence,v=>character.set('totaless',v),'Essence'));for(const key of ['karma','nuyen'])row(stats,key==='karma'?'Karma':'Nuyen',input(character.get(key)||'0',v=>character.set(key,v),key));
    if(skillCategory==='Magic'&&!magicSkillsApply(character))skillCategory='Active';
    const controls=el('div',undefined,'skills-filters');controls.setAttribute('role','group');controls.setAttribute('aria-label','Skill filters');
    const filter=button('Pool > 0',()=>{positive=!positive;render();});filter.setAttribute('aria-pressed',positive);controls.append(filter);
    for(const category of ['Combat','Active','Knowledge',...(magicSkillsApply(character)?['Magic']:[])]){const b=button(category,()=>{skillCategory=category;render();});b.setAttribute('aria-pressed',skillCategory===category);controls.append(b);}
    const skills=section('Skills',controls);skills.querySelector('h2').classList.add('skills-heading');
    for(const skill of filteredSkills(character,skillCategory,positive)){row(skills,skill.name,input(skill.rating,v=>character.setSkillRating(skill.index,v),skill.name+' rating'),'Pool '+skill.pool);}

  }
  if(page==='Combat'){
    const init=section('Initiative'),summary=el('div',undefined,'summary');init.append(summary);for(const [name,n] of [['Physical',`${character.initiative} / ${character.passes} IP`],['Matrix',`${character.matrix} / ${character.matrixPasses} IP`],...(character.magician?[['Astral',character.astral]]:[])]){const item=el('div',name);item.append(el('strong',n));summary.append(item);}
    for(const physical of [true,false]){const s=section(physical?'Physical condition':'Stun condition'),boxes=el('div',undefined,'monitor');s.append(boxes);for(let n=1;n<=character.capacity(physical);n++){const b=button(n%3===0?`−${n/3}`:String(n),async()=>{character.set(physical?'physicalcmfilled':'stuncmfilled',character.damage(physical)===n?n-1:n);await flush();render();});b.className=n<=character.damage(physical)?'filled':'';b.setAttribute('aria-label',`${physical?'Physical':'Stun'} damage ${n}`);boxes.append(b);}}
    $('main').append(el('p',`Wound modifier ${character.wounds}. Tap a box to set damage; tap the last filled box to heal one.`, 'muted'));
    weapons();if(character.magician||character.adept)list(character.magician?'Spells':'Adept powers',character.magician?'spells':'powers',character.magician?'spell':'power');if(character.magician)list('Spirits','spirits','spirit');list('Vehicles','vehicles','vehicle');
  }
  if(page==='Equipment'){
    weapons();const programNodes=new Set(character.programs.map(r=>r.node));if(programNodes.size){const programs=section('Programs · Loaded');for(const {node} of character.programs){const check=el('input');check.type='checkbox';check.checked=truth(value(node,'equipped'));check.setAttribute('aria-label','Loaded '+value(node,'name'));check.onchange=run(async()=>{character.setNode(node,'equipped',check.checked?'True':'False');await flush();});row(programs,value(node,'name')+(value(node,'rating')?' '+value(node,'rating'):''),check,source(node));}}
    const gear=section('Gear');for(const {node,depth} of character.gear.filter(r=>!programNodes.has(r.node))){row(gear,'　'.repeat(depth)+value(node,'name')+(value(node,'rating')&&value(node,'rating')!=='0'?' '+value(node,'rating'):''),value(node,'qty')&&'×'+value(node,'qty'),source(node));}
    list('Armor','armors','armor');list('Augmentations','cyberwares','cyberware');if(character.technomancer)list('Complex forms','complexforms','complexform');
  }
  if(page==='Background'){for(const [key,title] of [['background','Background'],['notes','Table notes']]){const s=section(title),area=el('textarea');area.value=key==='notes'?character.plainNotes:character.get(key);area.setAttribute('aria-label',title);area.oninput=()=>{key==='notes'?character.setPlainNotes(area.value):character.set(key,area.value);schedule();};area.onblur=run(flush);s.append(area);}}
  if(page==='Contacts'){for(const node of character.rows('contacts','contact')){let expanded=false;const more=button('Stats',()=>{expanded=!expanded;details.hidden=!expanded;more.setAttribute('aria-expanded',expanded);});more.setAttribute('aria-expanded','false');const s=section(value(node,'name')||'Contact',more);for(const key of ['connection','loyalty'])row(s,key[0].toUpperCase()+key.slice(1),input(value(node,key),v=>character.setNode(node,key,v),key));if(value(node,'notes'))s.append(el('div',value(node,'notes'),'prose'));const details=el('div');details.hidden=true;for(const item of [...node.children].filter(c=>!['name','connection','loyalty','notes','professionalrating','guid'].includes(c.tagName)&&!c.children.length&&c.textContent))row(details,item.tagName,item.textContent);s.append(details);}}
}
function list(title,collection,item){const rows=character.rows(collection,item);if(!rows.length)return;const s=section(title);for(const node of rows)row(s,value(node,'name')+(value(node,'extra')?' ('+value(node,'extra')+')':'')+(value(node,'rating')?' '+value(node,'rating'):''),'',source(node));}
function weapons(){const rows=character.rows('weapons','weapon');if(!rows.length)return;const s=section('Weapons');for(const node of rows)row(s,value(node,'name'),[character.weaponDamage(node),value(node,'mode'),value(node,'ap')&&'AP '+value(node,'ap')].filter(Boolean).join(' · '),source(node));}
$('characters').onchange=run(async e=>{const id=e.target.value;try{await select(id);}catch(error){e.target.value=record.id;throw error;}});
$('import').onclick=()=>$('file').click();$('file').onchange=run(async()=>{const file=$('file').files[0];if(!file)return;if(file.size>MAX_BYTES)throw new Error('Character exceeds 32 MB.');await add(new Uint8Array(await file.arrayBuffer()),file.name);$('file').value='';});
$('save-chum').onclick=run(async()=>{await flush();const bytes=character.bytes(true),url=URL.createObjectURL(new Blob([bytes],{type:'application/xml'})),a=el('a');a.href=url;a.download=record.filename||character.title+'.chum';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});
$('export').onclick=()=>$('exports').showModal();
let printURL;
function printCharacter(official){
 if(!character)return;
 const snapshot=new Character(character.bytes(true));
 // Open during the click gesture, before asynchronous PDF work, for mobile popup policies.
 const preview=window.open('about:blank','_blank');
 if(preview){preview.opener=null;preview.document.title='Preparing character sheet';preview.document.body.textContent='Preparing character sheet…';}
 return (async()=>{
  $('print-status').textContent='Preparing PDF…';
  for(const id of ['print-full','print-official'])$(id).disabled=true;
  try{
   await flush();const {characterPDF}=await import('./printing.mjs');const bytes=await characterPDF(snapshot,{official});
   if(printURL)URL.revokeObjectURL(printURL);
   printURL=URL.createObjectURL(new Blob([bytes],{type:'application/pdf'}));
   const filename=snapshot.title.replace(/[^\p{L}\p{N} ._-]/gu,'_')+(official?' - Official SR4A':' - Full character')+'.pdf';
   $('pdf-ready').href=printURL;$('pdf-ready').hidden=false;$('pdf-download').href=printURL;$('pdf-download').download=filename;$('pdf-download').hidden=false;
   $('print-status').textContent='PDF ready. Use Print in the PDF viewer, or save a copy.';
   if(preview&&!preview.closed)preview.location.replace(printURL);
  }catch(error){if(preview&&!preview.closed)preview.close();$('print-status').textContent=error.message;throw error;}
  finally{for(const id of ['print-full','print-official'])$(id).disabled=false;}
 })();
}
$('print-full').onclick=run(()=>printCharacter(false));
$('print-official').onclick=run(()=>printCharacter(true));
$('github').onclick=run(async()=>{$('repo').value=await db.setting('repository')||'';$('repository').showModal();});
let browseGeneration=0,previewCache=new Map(),choiceViews=new Map();
function clearPreviews(){browseGeneration++;for(const preview of previewCache.values())if(preview.image)URL.revokeObjectURL(preview.image);previewCache.clear();choiceViews.clear();remoteRows=[];$('remote').replaceChildren();}
function remoteList(){
 const target=$('remote');target.replaceChildren();choiceViews.clear();const filtered=remoteRows.filter(item=>matchesCharacter(item,$('search').value));
 for(const item of filtered){
  const b=button('',async()=>{const active=client;if(!active)throw new Error('Connect first.');b.disabled=true;try{const existing=records.find(r=>r.id===record?.id&&sameRemote(r.remote,{repoId:active.id,path:item.path}))||[...records].sort((a,b)=>b.updatedAt-a.updatedAt).find(r=>sameRemote(r.remote,{repoId:active.id,path:item.path}));if(existing){await select(existing.id);$('open-repository').close();status('Switched to saved local character · Edits retained');return;}const file=await active.read(item.path);if(!file)throw new Error('Character was removed. Refresh the list.');if(client!==active)throw new Error('Repository connection changed. Open again.');await add(file.bytes,item.name,{repoId:active.id,path:item.path,sha:file.sha,hash:await fingerprint(file.bytes)});$('open-repository').close();}finally{b.disabled=false;}});b.className='character-choice';
  paintChoice(b,item);choiceViews.set(item.path,b);target.append(b);
 }
 if(!filtered.length)target.append(el('p',remoteRows.length?'No matching characters.':'No .chum characters found in this repository.','muted'));
}
function paintChoice(b,item){b.replaceChildren();
  if(item.preview?.image){const image=el('img');image.src=item.preview.image;image.alt='';image.width=56;image.height=56;image.loading='lazy';b.append(image);}else b.append(el('span',(item.preview?.title||item.name).slice(0,2).toUpperCase(),'portrait-placeholder'));
  const label=el('span',undefined,'character-label');label.append(el('strong',item.preview?.title||item.name.replace(/\.chum$/i,'')));if(item.preview?.name&&item.preview.name!==item.preview.title)label.append(el('span',item.preview.name));label.append(el('small',item.path));b.append(label);
}
async function refreshCharacters(){
 if(!client)return;const active=client,generation=++browseGeneration;$('refresh-characters').disabled=true;$('repository-status').textContent='Refreshing repository…';remoteRows=[];$('remote').replaceChildren();
 try{
  const found=await active.list();if(generation!==browseGeneration||active!==client)return;
  const valid=new Set(found.map(item=>active.id+':'+item.sha));for(const [key,preview] of previewCache)if(!valid.has(key)){if(preview.image)URL.revokeObjectURL(preview.image);previewCache.delete(key);}
  remoteRows=found.map(item=>({...item,preview:previewCache.get(active.id+':'+item.sha)}));remoteList();$('repository-status').textContent=found.length+' characters · '+active.repository;
  let next=0;const workers=Array.from({length:Math.min(3,found.length)},async()=>{while(next<Math.min(remoteRows.length,100)&&generation===browseGeneration&&$('open-repository').open){const item=remoteRows[next++];if(item.preview)continue;let preview;try{if(item.size>MAX_BYTES)continue;preview=await characterPreview(await active.readBlob(item.sha));}catch(error){if(error.status===401||error.status===403||error.status===429){next=remoteRows.length;$('repository-status').textContent='Character list loaded. Portrait previews paused; '+error.message;}continue;}
    if(generation!==browseGeneration||active!==client||!$('open-repository').open){if(preview.image)URL.revokeObjectURL(preview.image);return;}
    const key=active.id+':'+item.sha,cached=previewCache.get(key);if(cached){if(preview.image)URL.revokeObjectURL(preview.image);preview=cached;}else previewCache.set(key,preview);item.preview=preview;if($('search').value)remoteList();else if(choiceViews.has(item.path))paintChoice(choiceViews.get(item.path),item);
   }});await Promise.all(workers);
 }catch(error){if(generation===browseGeneration){$('repository-status').textContent=error.message;throw error;}}
 finally{if(generation===browseGeneration)$('refresh-characters').disabled=false;}
}
async function openRepository(){
 await flush();if(!client){$('repo').value=await db.setting('repository')||'';status('Connect to your private repository to browse its characters.');$('repository').showModal();return;}
 $('search').value='';$('open-repository').showModal();await refreshCharacters();
}
$('open').onclick=run(openRepository);
$('refresh-characters').onclick=run(refreshCharacters);
$('open-repository').addEventListener('close',()=>{browseGeneration++;$('refresh-characters').disabled=false;});
$('connect').onclick=run(async()=>{const next=new GitHub($('repo').value,$('token').value);await next.verify();clearPreviews();client=next;$('token').value='';await db.setting('repository',next.repository);$('repository').close();status('Connected to private repository '+next.repository);await openRepository();void syncer.tick();});
$('search').oninput=remoteList;
$('disconnect').onclick=()=>{client=undefined;clearPreviews();$('token').value='';syncNote='GitHub: connect to sync';status('GitHub disconnected. Local characters retained.');};
function acceptAck(latest){records=records.map(r=>r.id===latest.id?latest:r);if(record?.id===latest.id)record={...record,remote:latest.remote};conflicts.delete(latest.id);pendingConflicts.delete(latest.id);}
function flagConflict(item,error){const existing=conflicts.has(item.id);conflicts.set(item.id,{id:item.id,repoId:error.repoId||item.remote?.repoId||client?.id,path:error.path||item.remote?.path||item.pendingRemotePath||`characters/${item.id}.chum`,message:error.message});if(!existing)pendingConflicts.add(item.id);syncNote='GitHub conflict · Local edits retained';status('Saved on this device',true);queueMicrotask(showNextConflict);}
const syncer=new AutoSync({client:()=>client,flush,list:db.all,acknowledge:db.acknowledge,validate:bytes=>new Character(bytes).bytes(true),onAck:acceptAck,onConflict:flagConflict,report:(message,error=false)=>{syncNote=message;status('Saved on this device',error);}});
function showNextConflict(){if(!document.querySelector('dialog[open]')&&pendingConflicts.size)void showConflict(pendingConflicts.values().next().value);}
async function showConflict(id){
 const issue=conflicts.get(id),item=records.find(r=>r.id===id);if(!issue||!item)return;
 if(document.querySelector('dialog[open]'))return;
 currentConflict={...issue,expectedSha:undefined};pendingConflicts.delete(id);$('conflict-name').value=item.title+' (copy)';$('conflict-description').textContent=`${item.title} · ${issue.path}. Keep both by saving under a new name, overwrite GitHub with your local changes, or delete only the local copy.`;$('conflict-state').textContent='Checking the current GitHub version…';$('conflict-overwrite').disabled=true;$('sync-conflict').showModal();
 const active=client,context=currentConflict;
 try{if(!active||active.id!==issue.repoId)throw new Error('Reconnect this character’s GitHub repository to overwrite it.');const remote=await active.read(issue.path);if(currentConflict!==context)return;context.expectedSha=remote?.sha||null;$('conflict-state').textContent=remote?'Your local edits and GitHub cannot be combined automatically.':'The GitHub file was deleted. Overwrite GitHub will recreate it.';$('conflict-overwrite').disabled=false;}catch(error){if(currentConflict===context)$('conflict-state').textContent=error.message;}
}
for(const dialog of document.querySelectorAll('dialog'))dialog.addEventListener('close',()=>queueMicrotask(showNextConflict));
$('conflict-later').onclick=()=>$('sync-conflict').close();
async function resolveConflict(work){const context=currentConflict;if(!context)return;for(const id of ['conflict-new','conflict-overwrite','conflict-delete','conflict-later'])$(id).disabled=true;try{await syncer.exclusive(async()=>{await flush();await work(context);});}catch(error){$('conflict-state').textContent=error.message;syncNote='GitHub: waiting to sync';status('Saved locally · '+error.message,true);if(error.conflict){context.expectedSha=undefined;$('conflict-state').textContent+=' Close this dialog and review the new GitHub version before overwriting.';}}finally{for(const id of ['conflict-new','conflict-delete','conflict-later'])$(id).disabled=false;$('conflict-overwrite').disabled=context.expectedSha===undefined;}}
$('conflict-overwrite').onclick=()=>resolveConflict(async context=>{
 if(!client||client.id!==context.repoId)throw new Error('Reconnect the original repository first.');if(!navigator.onLine)throw new Error('You are offline. Local changes remain saved.');
 const latest=(await db.all()).find(r=>r.id===context.id);if(!latest)throw new Error('This local copy no longer exists.');const bytes=new Character(latest.bytes).bytes(true),remote=await client.overwrite(context.path,bytes,context.expectedSha);acceptAck(await db.acknowledge(context.id,remote));syncNote='GitHub up to date';$('sync-conflict').close();status('Conflict resolved using local copy');
});
$('conflict-new').onclick=()=>resolveConflict(async context=>{
 const name=$('conflict-name').value.trim();if(!name)throw new Error('Enter a new character name.');const latest=(await db.all()).find(r=>r.id===context.id);if(!latest)throw new Error('This local copy no longer exists.');
 const model=new Character(latest.bytes);model.set('alias',name);const bytes=model.bytes(true),folder=context.path.includes('/')?context.path.slice(0,context.path.lastIndexOf('/')+1):'',stem=name.replace(/[^\p{L}\p{N} _.-]/gu,'_').slice(0,80)||'character',path=folder+stem+'-'+crypto.randomUUID().slice(0,8)+'.chum';
 const next={...latest,title:model.title,filename:path.split('/').at(-1),bytes,remote:undefined,pendingRepoId:context.repoId,pendingRemotePath:path,sourceHash:await fingerprint(bytes),updatedAt:Date.now()};await db.save(next);records=records.map(r=>r.id===next.id?next:r);if(record?.id===next.id){record=next;character=model;}render();conflicts.delete(next.id);pendingConflicts.delete(next.id);$('sync-conflict').close();syncNote='GitHub: waiting to sync';status('Saved under the new name locally');
 if(client&&client.id===context.repoId&&navigator.onLine){const remote=await client.saveNew(path,bytes);acceptAck(await db.acknowledge(next.id,remote));syncNote='GitHub up to date';status('Saved under the new name · Original GitHub file retained');}
});
$('conflict-delete').onclick=()=>{$('delete-local-state').textContent='';$('delete-local-confirm').showModal();};
$('delete-local-no').onclick=()=>$('delete-local-confirm').close();
$('delete-local-yes').onclick=run(async()=>{const context=currentConflict;if(!context)return;try{await syncer.exclusive(async()=>{await flush();await db.remove(context.id);records=records.filter(r=>r.id!==context.id);conflicts.delete(context.id);pendingConflicts.delete(context.id);if(record?.id===context.id){record=undefined;character=undefined;}render();$('delete-local-confirm').close();$('sync-conflict').close();status('Local copy deleted · GitHub file retained');});}catch(error){$('delete-local-state').textContent=error.message;}});
$('sync').onclick=run(()=>syncer.exclusive(async()=>{const attempted=record;try{if(!client)throw new Error('Connect to a private repository first.');if(!character)throw new Error('Import a character first.');await flush();const captured=record,revision=character.revision,bytes=character.bytes(true);status('Syncing snapshot…');const result=await client.sync(captured,bytes);await flush();if(result.pull){if(record?.id!==captured.id||character.revision!==revision)throw new Error('Edits arrived during sync. Local edits retained; sync again to resolve.');const replacement=new Character(result.pull);await db.save({...record,bytes:result.pull,title:replacement.title,remote:result.remote});character=replacement;record={...record,bytes:result.pull,title:replacement.title,remote:result.remote};}else{const updated=await db.acknowledge(captured.id,result.remote);if(updated)acceptAck(updated);}records=await db.all();render();status('Repository snapshot saved · Local edits retained');if($('open-repository').open)await refreshCharacters();}catch(error){if(attempted&&(error.conflict||error.status===409))flagConflict(attempted,error);else throw error;}}));
setInterval(()=>void syncer.tick(),60000);
window.addEventListener('online',()=>void syncer.tick());
document.addEventListener('visibilitychange',()=>{if(!document.hidden)void syncer.tick();});
document.addEventListener('focusout',()=>run(flush)());document.addEventListener('visibilitychange',()=>{if(document.hidden)run(flush)();});window.addEventListener('online',()=>status('Online · Local copies available'));window.addEventListener('offline',()=>status('Offline · Editing local copies'));
try{records=await db.all();if(records.length)syncNote='GitHub: reconnect to sync';const selected=await db.setting('selected');record=selected===undefined?records[0]:records.find(r=>r.id===selected);if(record)character=new Character(record.bytes);render();status('Saved on this device'+(navigator.onLine?'':' · Offline'));if('serviceWorker'in navigator)navigator.serviceWorker.register('./sw.js',{updateViaCache:'none'}).catch(()=>status('Local storage ready; offline app caching unavailable.',true));}catch(error){status(error.message,true);}
