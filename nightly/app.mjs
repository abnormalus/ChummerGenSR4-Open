import {Character,demo,value,integer,truth,MAX_BYTES,fingerprint} from './core.mjs';
import * as db from './storage.mjs';
import {GitHub} from './github.mjs';
const $=id=>document.getElementById(id);
const el=(tag,text,cls)=>{const node=document.createElement(tag);if(text!==undefined)node.textContent=text;if(cls)node.className=cls;return node;};
let records=[],record,character,page='Character',positive=false,client,remoteRows=[],saving=Promise.resolve(),timer;
function status(text,error=false){$('status').textContent=text;$('status').className=error?'error':'';}
function run(fn){return async(...args)=>{try{await fn(...args);}catch(error){status(error.message,true);}};}
function button(text,fn){const b=el('button',text);b.type='button';b.onclick=run(fn);return b;}
function section(title,action){const s=el('section'),h=el('h2',title);if(action)h.append(action);s.append(h);$('main').append(s);return s;}
function row(parent,name,detail,source){const r=el('div',undefined,'row');r.append(el('span',name,'name'));if(detail instanceof Node)r.append(detail);else if(detail)r.append(el('span',detail));if(source)r.append(el('small',source));parent.append(r);return r;}
function source(node){return [value(node,'source'),value(node,'page')&&'p. '+value(node,'page')].filter(Boolean).join(' ');}
function input(text,change,label){const i=el('input');i.type='text';i.value=text;i.setAttribute('aria-label',label);i.oninput=()=>{change(i.value);schedule();};i.onchange=run(async()=>{await flush();});return i;}
function schedule(){status('Saving on this device…');clearTimeout(timer);timer=setTimeout(()=>run(flush)(),200);}
async function flush(){clearTimeout(timer);if(!record||!character)return;const snapshot={...record,title:character.title,bytes:character.bytes(),updatedAt:Date.now()};record=snapshot;const operation=saving.then(()=>db.save(snapshot));saving=operation.catch(()=>{});await operation;records=records.map(r=>r.id===snapshot.id?snapshot:r);status('Saved on this device'+(navigator.onLine?'':' · Offline'));}
async function select(id){await flush();const next=records.find(r=>r.id===id);if(!next)return;character=new Character(next.bytes);record=next;await db.setting('selected',id);render();}
async function add(bytes,filename,remote){const model=new Character(bytes);await flush();const next={id:crypto.randomUUID(),filename,title:model.title,bytes,updatedAt:Date.now(),...(remote?{remote}:{})};await db.save(next);records.push(next);record=next;character=model;await db.setting('selected',next.id);render();status('Imported · Saved on this device');navigator.storage?.persist?.();}
function render(){
  const picker=$('characters');picker.replaceChildren();for(const r of records){const option=el('option',r.title);option.value=r.id;option.selected=r.id===record?.id;picker.append(option);}
  $('export').disabled=!character;$('main').replaceChildren();const nav=document.querySelector('nav');nav.replaceChildren();
  if(!character){const box=el('div',undefined,'empty'),icon=el('img');icon.src='./assets/icon.png';icon.alt='';box.append(icon,el('h1','SR 4 Dossier'),el('p','Your runner, ready for the table. Import a Chummer character or try a sample.'));box.append(button('Import .chum',()=>$('file').click()),document.createTextNode(' '),button('Try sample',()=>add(demo,'Nightjar.chum')));$('main').append(box);return;}
  for(const name of ['Character','Combat','Equipment','Background','Contacts']){const b=button(name,async()=>{await flush();page=name;render();window.scrollTo(0,0);});b.className=page===name?'active':'';nav.append(b);}
  if(page==='Character'){
    const identity=section('Dossier');for(const [key,label] of [['alias','Runner'],['name','Name'],['metatype','Metatype']]){const i=input(character.get(key),v=>character.set(key,v),label);i.className='wide';row(identity,label,i);}
    const stats=section('Attributes'),grid=el('div',undefined,'attributes');stats.append(grid);for(const a of character.attributes){const label=el('label',a.id);label.append(input(a.total,v=>character.setAttribute(a.id,'total',v),a.id+' total'));grid.append(label);}
    row(stats,'Essence',input(character.essence,v=>character.set('totaless',v),'Essence'));for(const key of ['karma','nuyen'])row(stats,key==='karma'?'Karma':'Nuyen',input(character.get(key)||'0',v=>character.set(key,v),key));
    const filter=button('Pool > 0',()=>{positive=!positive;render();});filter.setAttribute('aria-pressed',positive);const skills=section('Skills',filter);for(const skill of character.skills.filter(s=>!positive||s.pool>0).sort((a,b)=>a.name.localeCompare(b.name))){row(skills,skill.name,input(skill.rating,v=>character.setSkillRating(skill.index,v),skill.name+' rating'),'Pool '+skill.pool);}
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
function remoteList(){const target=$('remote');target.replaceChildren();for(const item of remoteRows.filter(r=>r.name.toLowerCase().includes($('search').value.toLowerCase())))target.append(button(item.name,async()=>{if(!client)throw new Error('Connect first.');const file=await client.read(item.path);if(!file)throw new Error('Character was removed.');await add(file.bytes,item.name,{repoId:client.id,path:item.path,sha:file.sha,hash:await fingerprint(file.bytes)});$('repository').close();}));if(!remoteRows.length)target.append(el('p','No .chum files in the characters folder yet.','muted'));}
$('connect').onclick=run(async()=>{const next=new GitHub($('repo').value,$('token').value);await next.verify();client=next;$('token').value='';await db.setting('repository',next.repository);remoteRows=await client.list();remoteList();status('Connected to private repository '+next.repository);});
$('search').oninput=remoteList;
$('disconnect').onclick=()=>{client=undefined;remoteRows=[];$('token').value='';$('remote').replaceChildren();status('GitHub disconnected. Local characters retained.');};
$('sync').onclick=run(async()=>{if(!client)throw new Error('Connect to a private repository first.');if(!character)throw new Error('Import a character first.');await flush();const captured=record,revision=character.revision,bytes=character.bytes(true);status('Syncing snapshot…');const result=await client.sync(captured,bytes);await flush();if(result.pull){if(record.id!==captured.id||character.revision!==revision)throw new Error('Edits arrived during sync. Local edits retained; sync again to resolve.');const replacement=new Character(result.pull);await db.save({...record,bytes:result.pull,title:replacement.title,remote:result.remote});character=replacement;record={...record,bytes:result.pull,title:replacement.title,remote:result.remote};}else{const updated=await db.acknowledge(captured.id,result.remote);if(record.id===captured.id)record=updated;}records=await db.all();render();status('Repository snapshot saved · Local edits retained');remoteRows=await client.list();remoteList();});
document.addEventListener('focusout',()=>run(flush)());document.addEventListener('visibilitychange',()=>{if(document.hidden)run(flush)();});window.addEventListener('online',()=>status('Online · Local copies available'));window.addEventListener('offline',()=>status('Offline · Editing local copies'));
try{records=await db.all();const selected=await db.setting('selected');record=records.find(r=>r.id===selected)||records[0];if(record)character=new Character(record.bytes);render();status('Saved on this device'+(navigator.onLine?'':' · Offline'));if('serviceWorker'in navigator)navigator.serviceWorker.register('./sw.js').catch(()=>status('Local storage ready; offline app caching unavailable.',true));}catch(error){status(error.message,true);}
