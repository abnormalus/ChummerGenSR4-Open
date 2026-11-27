import {Character,demo,value,integer,truth,MAX_BYTES,fingerprint} from './core.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import * as db from './storage.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {GitHub} from './github.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {characterPreview,matchesCharacter} from './repository-browser.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {installController} from './install.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {existingCharacter,sameRemote} from './identity.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {weaponPool,weaponRanges,attachments,loadedAmmo,activeSlot,conditionPenalty,isGrenade,grenadeLots,ammunition,fireCounts,setActiveSlot,fireWeapon,reloadWeapon,useGrenade} from './weapons.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {armorRating,armorMods,armorProtections,defensePools} from './armor.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {augmentationEssence,augmentationEffects,augmentationChildren} from './augmentations.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {AutoSync} from './autosync.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {filteredSkills,magicSkillsApply,skillRating,perceptionSense,perceptionDifferences} from './skill-filters.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
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
// Explicit refresh keeps local dossiers intact and activates a fully cached update.
$('refresh-app').onclick=run(async()=>{
 const header=document.querySelector('header'),main=$('main');header.inert=true;main.inert=true;
 try{
  await flush();
  if(!navigator.onLine)throw Error('Changes saved on this device. Connect to the internet to refresh the app.');
  status('Changes saved · Checking for the latest app version…');
  if('serviceWorker' in navigator){
   const registration=await navigator.serviceWorker.getRegistration();
   if(registration){
    await registration.update();
    const worker=registration.installing;
    if(worker)await new Promise((resolve,reject)=>{
     const timeout=setTimeout(()=>{cleanup();reject(Error('Update timed out. Changes are saved; please try Refresh app again.'));},45000);
     function cleanup(){clearTimeout(timeout);worker.removeEventListener('statechange',changed);}
     function changed(){if(worker.state==='installed'){cleanup();resolve();}else if(worker.state==='redundant'){cleanup();reject(Error('App update failed. Changes are saved; please try again.'));}}
     worker.addEventListener('statechange',changed);changed();
    });
    if(registration.waiting)await new Promise((resolve,reject)=>{
     const timeout=setTimeout(()=>{cleanup();reject(Error('Update activation timed out. Changes are saved; please try again.'));},15000);
     function cleanup(){clearTimeout(timeout);navigator.serviceWorker.removeEventListener('controllerchange',changed);}
     function changed(){cleanup();resolve();}
     navigator.serviceWorker.addEventListener('controllerchange',changed);
     registration.waiting.postMessage({type:'SKIP_WAITING'});
    });
   }
  }
  // Revalidate the document as well as its versioned module graph.
  const latest=new URL(location.href);latest.searchParams.set('refresh',Date.now());location.replace(latest.href);
 }finally{header.inert=false;main.inert=false;}
});
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
    const stats=section('Attributes'),grid=el('div',undefined,'attributes');stats.append(grid);for(const a of character.attributes.filter(a=>a.id!=='ESS')){const label=el('label',a.id);label.append(input(a.total,v=>character.setAttribute(a.id,'total',v),a.id+' total'));grid.append(label);}
    row(stats,'Essence',input(character.essence,v=>character.set('totaless',v),'Essence'));for(const key of ['karma','nuyen'])row(stats,key==='karma'?'Karma':'Nuyen',input(character.get(key)||'0',v=>character.set(key,v),key));
    if(skillCategory==='Magic'&&!magicSkillsApply(character))skillCategory='Active';
    const controls=el('div',undefined,'skills-filters');controls.setAttribute('role','group');controls.setAttribute('aria-label','Skill filters');
    const filter=button('Pool > 0',()=>{positive=!positive;render();});filter.setAttribute('aria-pressed',positive);controls.append(filter);
    for(const category of ['Combat','Active','Knowledge',...(magicSkillsApply(character)?['Magic']:[])]){const b=button(category,()=>{skillCategory=skillCategory===category?null:category;render();});b.setAttribute('aria-pressed',skillCategory===category);controls.append(b);}
    const skills=section('Skills',controls);skills.querySelector('h2').classList.add('skills-heading');
    for(const skill of filteredSkills(character,skillCategory,positive)){if(perceptionSense(skill)&&character.skills.some(s=>s.name==='Perception'))continue;row(skills,skill.name,input(skillRating(skill),v=>character.setSkillRating(skill.index,value(skill.node,'skillcategory')==='Language'&&v.trim().toUpperCase()==='N'?0:v),skill.name+' rating'),'Pool '+skill.pool);if(skill.name==='Perception')for(const sense of perceptionDifferences(character,skill)){const line=row(skills,sense.name,'Pool '+sense.pool);line.classList.add('skill-sense');}}

  }
  if(page==='Combat'){
    combatDefence();
    const init=section('Initiative'),summary=el('div',undefined,'summary');init.append(summary);for(const [name,n] of [['Physical',`${character.initiative} / ${character.passes} IP`],['Matrix',`${character.matrix} / ${character.matrixPasses} IP`],...(character.magician?[['Astral',character.astral]]:[])]){const item=el('div',name);item.append(el('strong',n));summary.append(item);}
    for(const physical of [true,false]){const s=section(physical?'Physical condition':'Stun condition'),boxes=el('div',undefined,'monitor');s.append(boxes);for(let n=1;n<=character.capacity(physical);n++){const b=button(String(n),async()=>{character.set(physical?'physicalcmfilled':'stuncmfilled',character.damage(physical)===n?n-1:n);await flush();render();});b.className=n<=character.damage(physical)?'filled':'';const penalty=conditionPenalty(character,physical,n);if(penalty){const corner=el('span','−'+Math.abs(penalty),'wound-penalty');corner.setAttribute('aria-hidden','true');b.append(corner);}b.setAttribute('aria-label',`${physical?'Physical':'Stun'} damage ${n}`);if(penalty)b.setAttribute('aria-description',`Wound penalty ${penalty}`);boxes.append(b);}}
    $('main').append(el('p',`Wound modifier ${character.wounds}. Tap a box to set damage; tap the last filled box to heal one.`, 'muted'));
    weapons();if(character.magician||character.adept)list(character.magician?'Spells':'Adept powers',character.magician?'spells':'powers',character.magician?'spell':'power');if(character.magician)list('Spirits','spirits','spirit');vehicles();
  }
  if(page==='Equipment'){
    weapons();const programNodes=new Set(character.programs.map(r=>r.node));if(programNodes.size){const programs=section('Programs · Loaded');for(const {node} of character.programs){const check=el('input');check.type='checkbox';check.checked=truth(value(node,'equipped'));check.setAttribute('aria-label','Loaded '+value(node,'name'));check.onchange=run(async()=>{character.setNode(node,'equipped',check.checked?'True':'False');await flush();});row(programs,value(node,'name')+(value(node,'rating')?' '+value(node,'rating'):''),check,source(node));}}
    const assignedAmmo=new Set(character.rows('weapons','weapon').flatMap(w=>[...ammunition(character,w),...grenadeLots(character,w)]));const gear=section('Gear');for(const {node,depth} of character.gear.filter(r=>!programNodes.has(r.node)&&!assignedAmmo.has(r.node))){const gearRow=row(gear,'　'.repeat(depth)+value(node,'name')+(value(node,'rating')&&value(node,'rating')!=='0'?' '+value(node,'rating'):''),value(node,'qty')&&'×'+value(node,'qty'),source(node));if(isGrenade(node)){const use=button('Use',()=>weaponChange(()=>useGrenade(character,node)));use.setAttribute('aria-label','Use '+value(node,'name'));use.disabled=Number(value(node,'qty'))<1;gearRow.prepend(use);}}
    vehicles();armorPanel();augmentations();if(character.technomancer)list('Complex forms','complexforms','complexform');
  }
  if(page==='Background'){for(const [key,title] of [['background','Background'],['notes','Table notes']]){const s=section(title),area=el('textarea');area.value=key==='notes'?character.plainNotes:character.get(key);area.setAttribute('aria-label',title);area.oninput=()=>{key==='notes'?character.setPlainNotes(area.value):character.set(key,area.value);schedule();};area.onblur=run(flush);s.append(area);}}
  if(page==='Contacts'){for(const node of character.rows('contacts','contact')){let expanded=false;const more=button('Stats',()=>{expanded=!expanded;details.hidden=!expanded;more.setAttribute('aria-expanded',expanded);});more.setAttribute('aria-expanded','false');const s=section(value(node,'name')||'Contact',more);for(const key of ['connection','loyalty'])row(s,key[0].toUpperCase()+key.slice(1),input(value(node,key),v=>character.setNode(node,key,v),key));if(value(node,'notes'))s.append(el('div',value(node,'notes'),'prose'));const details=el('div');details.hidden=true;for(const item of [...node.children].filter(c=>!['name','connection','loyalty','notes','professionalrating','guid'].includes(c.tagName)&&!c.children.length&&c.textContent))row(details,item.tagName,item.textContent);s.append(details);}}
}
function combatDefence(){
 const s=section('Defence'),grid=el('div',undefined,'defence-grid');for(const item of defensePools(character)){const entry=el('div');entry.append(el('span',item.name),el('strong',String(item.pool)),el('small',item.formula));grid.append(entry);}s.append(grid);
 const armor=character.armor;row(s,'Damage resistance',`${character.total('BOD')+armor.b+character.improvement('DamageResistance')} Ballistic / ${character.total('BOD')+armor.i+character.improvement('DamageResistance')} Impact`,'BOD + armor · before incoming AP');armorPanel();
}
function armorPanel(){
 const all=character.rows('armors','armor');if(!all.length)return;const s=section('Armor'),state=character.armor,details=el('details',undefined,'armor-selection');details.dataset.key='worn-armor';details.append(el('summary',`Worn armor · ${state.b}/${state.i} · AGI/REA ${state.penalty||'0'}`));row(details,'Encumbrance',`${state.loadB}/${state.loadI} · Threshold ${state.threshold}`,'SR4A p. 161');
 for(const node of all){const check=el('input');check.type='checkbox';check.checked=state.worn.includes(node);check.setAttribute('aria-label','Wear '+value(node,'name'));check.onchange=run(async()=>{const checked=check.checked;await weaponChange(()=>{for(const armor of all)character.setNode(armor,'equipped',state.worn.includes(armor)?'True':'False');character.setField('armorSelectionExplicit','True');character.setNode(node,'equipped',checked?'True':'False');});});const r=row(details,value(node,'armorname')||value(node,'name'),`${armorRating(node,'b')}/${armorRating(node,'i')}`,source(node));r.prepend(check);for(const mod of armorMods(node))row(details,'　'+value(mod,'name')+(Number(value(mod,'rating'))?' '+value(mod,'rating'):''),'',source(mod));}s.append(details);
 for(const protection of armorProtections(character))row(s,protection.name+(protection.rating?' '+protection.rating:''),protection.armor,protection.source);
}
function augmentations(){
 const nodes=character.rows('cyberwares','cyberware');if(!nodes.length)return;const s=section('Augmentations');
 function display(node,parent){const detail=el('details',undefined,'augmentation-details'),rating=Number(value(node,'rating'));detail.append(el('summary',value(node,'name')+(rating?' '+rating:'')+(value(node,'extra')?' ('+value(node,'extra')+')':'')));row(detail,'Essence',augmentationEssence(character,node),source(node));if(value(node,'grade'))row(detail,'Grade',value(node,'grade'));for(const effect of augmentationEffects(character,node))row(detail,effect.name,effect.amount);for(const nested of augmentationChildren(node))display(nested,detail);for(const gear of node.querySelectorAll(':scope > gears > gear'))row(detail,value(gear,'name')+(Number(value(gear,'rating'))?' '+value(gear,'rating'):''),'',source(gear));if(value(node,'notes'))detail.append(el('p',value(node,'notes'),'prose'));parent.append(detail);}for(const node of nodes)display(node,s);
}
function list(title,collection,item){const rows=character.rows(collection,item);if(!rows.length)return;const s=section(title);for(const node of rows)row(s,value(node,'name')+(value(node,'extra')?' ('+value(node,'extra')+')':'')+(value(node,'rating')?' '+value(node,'rating'):''),'',source(node));}
async function weaponChange(action,index,weapon){const opened=[...document.querySelectorAll('details[data-key][open]')].map(n=>n.dataset.key);action();await flush();render();for(const n of document.querySelectorAll('details[data-key]'))if(opened.includes(n.dataset.key))n.open=true;if(weapon){const key='weapon-'+(value(weapon,'guid')||value(weapon,'name'));const detail=[...document.querySelectorAll('details[data-key]')].find(n=>n.dataset.key===key);if(detail)detail.open=true;}if(index!==undefined){const detail=$('main').querySelectorAll('.weapon-details')[index];if(detail)detail.open=true;}}
function weaponDialog(title,build){const dialog=el('dialog'),heading=el('h2',title),error=el('p',undefined,'error');error.setAttribute('role','status');const close=button('Close',()=>dialog.close());dialog.append(heading);const action=(name,fn)=>button(name,async()=>{try{await fn();dialog.close();}catch(e){error.textContent=e.message;}});build(dialog,action);dialog.append(error,close);dialog.addEventListener('close',()=>dialog.remove());document.body.append(dialog);dialog.showModal();}
function reloadDialog(weapon,index,selected){weaponDialog('Reload '+value(weapon,'name'),(dialog,action)=>{
 const slot=el('select');slot.setAttribute('aria-label','Magazine');for(let n=1;n<=4;n++){const option=el('option','Magazine '+n);option.value=n;slot.append(option);}slot.value=activeSlot(character,weapon);
 const reserve=el('select');reserve.setAttribute('aria-label','Ammunition reserve');const lots=ammunition(character,weapon);for(const [n,ammo] of lots.entries()){const option=el('option',value(ammo,'name')+' · Reserve '+value(ammo,'qty'));option.value=n;reserve.append(option);}if(selected)reserve.value=lots.indexOf(selected);
 const count=el('input');count.type='number';count.min='1';count.step='1';count.setAttribute('aria-label','Rounds to load');const capacity=value(weapon,'ammo').match(/^(\d+)/);count.value=capacity?capacity[1]:'1';
 for(const [label,control] of [['Magazine',slot],['Ammunition',reserve],['Rounds to load',count]]){const field=el('label',label);field.append(control);dialog.append(field);}
 const load=action('Load',()=>weaponChange(()=>reloadWeapon(character,weapon,Number(slot.value),lots[Number(reserve.value)],Number(count.value)),index,weapon));load.disabled=!lots.length;
 dialog.append(load,action('Unload',()=>weaponChange(()=>reloadWeapon(character,weapon,Number(slot.value),null,0),index,weapon)));
});}
function weapons(nodes=character.rows('weapons','weapon'),parent){
 if(!nodes.length)return;const s=parent||section('Weapons');
 const labels={rc:'Recoil compensation',reach:'Reach',notes:'Notes',dvbonus:'Damage bonus',apbonus:'AP bonus',dicepool:'Pool modifier',rcbonus:'Recoil bonus',fullburst:'Full burst',suppressive:'Suppressive fire',damage:'Damage',ap:'AP',pool:'Pool modifier',damagetype:'Damage type',damagereplace:'Damage replacement',apreplace:'AP replacement'};
 function fields(parent,node,allowed){for(const key of allowed){const text=value(node,key);if(text&&text!=='0'&&text!=='False')row(parent,labels[key]||key,text);}}
 for(const [localIndex,node] of nodes.entries()){const index=parent?undefined:localIndex;
  const details=el('details',undefined,'weapon-details');details.dataset.key='weapon-'+(value(node,'guid')||value(node,'name'));const summary=el('summary'),pool=weaponPool(character,node),name=el('span',value(node,'weaponname')||value(node,'name'),'name'),grenades=grenadeLots(character,node);
  function quick(label,fn){const b=button(label,fn);const handler=b.onclick;b.onclick=e=>{e.preventDefault();e.stopPropagation();return handler(e);};return b;}
  if(grenades.length){const use=quick('Use',()=>weaponChange(()=>useGrenade(character,grenades.find(n=>Number(value(n,'qty'))>=1)),index));use.setAttribute('aria-label','Use '+value(node,'name'));use.disabled=!grenades.some(n=>Number(value(n,'qty'))>=1);summary.append(use);}
  summary.append(name,el('span',[character.weaponDamage(node),...(page==='Combat'?['Pool '+pool.text]:[]),value(node,'mode'),value(node,'ap')&&'AP '+value(node,'ap'),grenades.length?'×'+grenades.reduce((n,g)=>n+Number(value(g,'qty')||0),0):''].filter(Boolean).join(' · '),'weapon-stats'));if(source(node))summary.append(el('small',source(node)));
  const ranged=value(node,'type').toLowerCase()==='ranged'&&value(node,'requireammo').toLowerCase()!=='false'&&value(node,'ammo')!=='0'&&!grenades.length;
  if(ranged){const active=activeSlot(character,node),remaining=integer(value(node,'ammoremaining'+(active===1?'':active)));const actions=el('span',undefined,'weapon-fire-actions');for(const count of fireCounts(node)){const fire=quick('Fire '+count,()=>weaponChange(()=>fireWeapon(character,node,active,count),index,node));fire.classList.add('fire-button');fire.setAttribute('aria-label','Fire '+count+' from '+value(node,'name'));fire.disabled=remaining<count;actions.append(fire);}const reload=quick('Reload',()=>reloadDialog(node,index));reload.setAttribute('aria-label','Reload '+value(node,'name'));actions.append(reload);summary.append(actions);}details.append(summary);
  if(page!=='Combat')row(details,'Dice pool',pool.text);details.append(el('p',pool.description,'muted weapon-note'));
  const bands=weaponRanges(character,node);if(bands.length)row(details,'Range (m)',bands.map(b=>b.text.match(/–([\d.]+)/)?.[1]||b.text).join('/'),'Short / Medium / Long / Extreme');
  fields(details,node,['rc','reach','notes']);if(ranged&&value(node,'ammo'))row(details,'Capacity',value(node,'ammo'));
  if(ranged){const active=activeSlot(character,node);for(let slot=1;slot<=4;slot++){const suffix=slot===1?'':slot,ammo=loadedAmmo(character,node,slot),remaining=value(node,'ammoremaining'+suffix);if(slot!==active&&!ammo&&(!remaining||remaining==='0'))continue;const chosen=slot;const select=button(`${ammo?value(ammo,'name'):'Ammo type not saved'} · ${remaining||'0'} rounds${slot===active?' · Active':''}`,()=>weaponChange(()=>setActiveSlot(character,node,chosen),index));select.setAttribute('aria-label','Select magazine '+slot+' for '+value(node,'name'));row(details,'Magazine '+slot,select);if(ammo){const bonus=ammo.querySelector('weaponbonus');if(bonus)fields(details,bonus,['damage','ap','pool','damagetype','damagereplace','apreplace']);}}}
  for(const mod of attachments(node).filter(n=>truth(value(n,'installed')))){const rating=value(mod,'rating');row(details,value(mod,'name')+(rating&&rating!=='0'?' '+rating:''),[value(mod,'dicepool')?'Pool '+value(mod,'dicepool'):'',value(mod,'dvbonus')&&value(mod,'dvbonus')!=='0'?'DV '+value(mod,'dvbonus'):'',value(mod,'apbonus')&&value(mod,'apbonus')!=='0'?'AP '+value(mod,'apbonus'):''].filter(Boolean).join(' · '),source(mod));}
  s.append(details);
  for(const ammo of ammunition(character,node)){const load=button('Load',()=>reloadDialog(node,index,ammo));load.setAttribute('aria-label','Load '+value(ammo,'name')+' into '+value(node,'name'));const r=row(s,value(ammo,'name')+' · Reserve '+value(ammo,'qty'),load,source(ammo));r.classList.add('weapon-ammo');}
 }
}
function vehicles(){
 const nodes=character.rows('vehicles','vehicle');if(!nodes.length)return;const sectionNode=section('Vehicles');
 for(const [index,vehicle] of nodes.entries()){
  const details=el('details',undefined,'vehicle-details');details.dataset.key='vehicle-'+(value(vehicle,'guid')||index);details.append(el('summary',value(vehicle,'vehiclename')||value(vehicle,'name')));if(value(vehicle,'vehiclename'))row(details,'Model',value(vehicle,'name'),source(vehicle));else if(source(vehicle))details.append(el('p',source(vehicle),'muted weapon-note'));
  const capacity=Math.max(0,8+Math.ceil(integer(value(vehicle,'body'))/2)),damage=Math.max(0,integer(value(vehicle,'physicalcmfilled'))),monitor=el('div',undefined,'monitor');row(details,'Physical condition',`${damage}/${capacity}`);for(let n=1;n<=capacity;n++){const box=button(String(n),()=>weaponChange(()=>character.setNode(vehicle,'physicalcmfilled',damage===n?n-1:n)));box.setAttribute('aria-label',`${value(vehicle,'vehiclename')||value(vehicle,'name')} physical damage ${n}`);box.className=n<=damage?'filled':'';monitor.append(box);}details.append(monitor);
  for(const [key,title] of [['handling','Handling'],['accel','Acceleration'],['speed','Speed'],['pilot','Pilot'],['body','Body'],['armor','Armor'],['sensor','Sensor'],['devicerating','Device rating'],['response','Response'],['system','System'],['firewall','Firewall'],['signal','Signal'],['seats','Seats']])if(value(vehicle,key))row(details,title,value(vehicle,key));
  const mods=[...vehicle.querySelectorAll(':scope > mods > mod, :scope > weaponmounts > weaponmount')];
  for(const [mi,mod] of mods.entries()){const group=el('details',undefined,'weapon-extra');group.dataset.key=details.dataset.key+'-mount-'+mi;group.append(el('summary',value(mod,'name')+(value(mod,'rating')&&value(mod,'rating')!=='0'?' '+value(mod,'rating'):'')));if(source(mod))group.append(el('p',source(mod),'muted weapon-note'));for(const key of ['extra','slots','response','system','firewall','signal','pilot','control','flexibility','visibility','size','notes'])if(value(mod,key)&&value(mod,key)!=='0')row(group,key,value(mod,key));const mounted=[...mod.querySelectorAll('weapon')];if(mounted.length)weapons(mounted,group);else if(/mount|hardpoint|turret/i.test(value(mod,'name')))row(group,'Weapon','No weapon mounted');details.append(group);}
  const mounted=[...vehicle.querySelectorAll(':scope > weapons > weapon')];if(mounted.length){row(details,'Mounted weapons','Manual Gunnery pools');weapons(mounted,details);}
  const allWeapons=[...vehicle.querySelectorAll('weapon')],assigned=new Set(allWeapons.flatMap(w=>ammunition(character,w)));
  const cargo=[...vehicle.querySelectorAll('gear')].filter(n=>!assigned.has(n));if(cargo.length){row(details,'Cargo / accessories',String(cargo.length));for(const gear of cargo)row(details,value(gear,'name')+(value(gear,'rating')&&value(gear,'rating')!=='0'?' '+value(gear,'rating'):''),value(gear,'qty')?'×'+value(gear,'qty'):'',source(gear));}
  if(value(vehicle,'notes'))details.append(el('p',value(vehicle,'notes'),'prose'));sectionNode.append(details);
 }
}
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
   await flush();const {characterPDF}=await import('./printing.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6');const bytes=await characterPDF(snapshot,{official});
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
async function saveTokenPreference(){if($('remember-token').checked&&$('token').value.trim())await db.setting('githubToken',$('token').value.trim());else await db.forgetSetting('githubToken');}
$('remember-token').onchange=run(saveTokenPreference);$('token').onchange=run(saveTokenPreference);
$('connect').onclick=run(async()=>{const next=new GitHub($('repo').value,$('token').value);await next.verify();clearPreviews();client=next;await saveTokenPreference();if(!$('remember-token').checked)$('token').value='';await db.setting('repository',next.repository);$('repository').close();status('Connected to private repository '+next.repository);await openRepository();void syncer.tick();});
$('search').oninput=remoteList;
$('disconnect').onclick=run(async()=>{client=undefined;clearPreviews();$('token').value='';$('remember-token').checked=false;await db.forgetSetting('githubToken');syncNote='GitHub: connect to sync';status('GitHub disconnected · Saved token removed. Local characters retained.');});
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
try{const savedToken=await db.setting('githubToken');if(typeof savedToken==='string'&&savedToken){$('token').value=savedToken;$('remember-token').checked=true;}records=await db.all();if(records.length)syncNote='GitHub: reconnect to sync';const selected=await db.setting('selected');record=selected===undefined?records[0]:records.find(r=>r.id===selected);if(record)character=new Character(record.bytes);render();status('Saved on this device'+(navigator.onLine?'':' · Offline'));if('serviceWorker'in navigator)navigator.serviceWorker.register('./sw.js',{updateViaCache:'none'}).catch(()=>status('Local storage ready; offline app caching unavailable.',true));}catch(error){status(error.message,true);}
