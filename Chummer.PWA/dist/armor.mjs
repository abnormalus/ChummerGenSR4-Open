// Sheet calculations use saved equipment; no catalog text is embedded.
const children=(n,k)=>[...(n?.children||[])].filter(c=>c.tagName===k),child=(n,k)=>children(n,k)[0],value=(n,k)=>child(n,k)?.textContent||'',num=s=>Number.isFinite(Number(s))?Number(s):0,truth=s=>String(s).toLowerCase()==='true';
const raw=(c,id)=>{const n=children(child(c.root,'attributes'),'attribute').find(n=>value(n,'name')===id);return num(value(n,'totalvalue')||value(n,'value'));};
export const armorMods=n=>children(child(n,'armormods'),'armormod').filter(m=>value(m,'equipped').toLowerCase()!=='false');
export function armorRating(n,key){return Math.max(0,num(value(n,key))+armorMods(n).reduce((sum,m)=>sum+num(value(m,key)),0)-num(value(n,key==='b'?'bdamage':'idamage')));}
export function wornArmor(c){const all=c.rows('armors','armor');const worn=all.filter(n=>truth(value(n,'equipped')));if(worn.length||c.field('armorSelectionExplicit')||!all.length)return worn;return [...all].sort((a,b)=>(armorRating(b,'b')+armorRating(b,'i'))-(armorRating(a,'b')+armorRating(a,'i'))||armorRating(b,'b')-armorRating(a,'b')).slice(0,1);}
export function armorEffectActive(c,effect){const source=value(effect,'improvementsource');if(!['Armor','ArmorMod'].includes(source))return true;const id=value(effect,'sourcename'),all=c.rows('armors','armor'),owner=all.find(n=>[n,...n.querySelectorAll('armormod')].some(p=>id.startsWith(value(p,'guid'))&&value(p,'guid')));return !owner||wornArmor(c).includes(owner);}
export function armorState(c){
 const worn=wornArmor(c),improvements=c.improvements;
 const sumType=type=>c.improvement(type);
 function rating(key){const ordinary=worn.filter(n=>!value(n,key).startsWith('+')),clothing=worn.filter(n=>value(n,key).startsWith('+')&&value(n,'category')==='Clothing'),addons=worn.filter(n=>value(n,key).startsWith('+')&&value(n,'category')!=='Clothing');return Math.max(0,...ordinary.map(n=>armorRating(n,key)),clothing.reduce((s,n)=>s+armorRating(n,key),0))+addons.reduce((s,n)=>s+armorRating(n,key),0);}
 const b=rating('b'),i=rating('i');const military=worn.some(n=>value(n,'category')==='Military Grade Armor');
 const option=k=>truth(c.get(k))||truth(value(child(c.root,'options'),k));
 const threshold=option('alternatearmorencumbrance')?raw(c,'BOD')*(military?2:1)+raw(c,'STR'):raw(c,'BOD')*(military?3:2);
 const soft=improvements.filter(n=>value(n,'improvementttype')==='SoftWeave').length;
 function load(key){return Math.max(0,worn.reduce((s,n)=>s+(value(n,'name').startsWith('Form-Fitting')?Math.floor(armorRating(n,key)/2):armorRating(n,key)),0)-soft*Math.min(key==='b'?b:i,raw(c,'STR')));}
 const loadB=load('b'),loadI=load('i'),count=worn.filter(n=>!['Helmets and Shields','SecureTech PPP System'].includes(value(n,'category'))).length;
 const penalty=option('ignorearmorencumbrance')||(option('nosinglearmorencumbrance')&&count===1)?0:Math.min(0,-Math.ceil(Math.max(0,loadB-threshold,loadI-threshold)/2)-((Math.max(loadB,loadI)>threshold)?sumType('ArmorEncumbrancePenalty'):0));
 return {worn,b:Math.max(0,b+sumType('BallisticArmor')),i:Math.max(0,i+sumType('ImpactArmor')),loadB,loadI,threshold,penalty};
}
export function importedArmorPenalty(c,id){return c.improvements.filter(n=>value(n,'improvementsource')==='ArmorEncumbrance'&&value(n,'improvementttype')==='Attribute'&&value(n,'improvedname')===id).reduce((sum,n)=>sum+num(value(n,'aug')),0);}
export function armorProtections(c){
 const result=[];for(const armor of armorState(c).worn)for(const mod of armorMods(armor)){const name=value(mod,'name'),rating=num(value(mod,'rating'));if(/chemical|fire resistance|insulation|nonconductivity|radiation|thermal|shock|insulat|resistan|protect|seal/i.test(name))result.push({name,rating,armor:value(armor,'name'),source:[value(mod,'source'),value(mod,'page')&&'p. '+value(mod,'page')].filter(Boolean).join(' ')});}return result;
}
export function defensePools(c){
 const reaction=c.total('REA'),wounds=c.wounds,rows=[{name:'Ranged defence',pool:Math.max(0,reaction+wounds),formula:'REA'}];
 function skill(name){const s=c.skills.find(s=>s.name===name);if(!s)return null;return value(s.node,'totalvalue')?Math.max(0,num(value(s.node,'totalvalue'))-raw(c,s.attribute)-c.importedWounds):Math.max(0,s.rating+(s.rating===0&&truth(value(s.node,'default'))?-1:0));}
 const dodge=skill('Dodge'),gym=skill('Gymnastics'),unarmed=skill('Unarmed Combat');
 if(dodge!==null){rows.push({name:'Full ranged dodge',pool:Math.max(0,reaction+dodge+wounds),formula:'REA + Dodge'},{name:'Melee dodge',pool:Math.max(0,reaction+dodge+wounds),formula:'REA + Dodge'},{name:'Full melee dodge',pool:Math.max(0,reaction+2*dodge+wounds),formula:'REA + Dodge × 2'});}
 if(gym!==null)rows.push({name:'Full gymnastics dodge',pool:Math.max(0,reaction+gym+wounds),formula:'REA + Gymnastics'});
 if(unarmed!==null)rows.push({name:'Melee block',pool:Math.max(0,reaction+unarmed+wounds),formula:'REA + Unarmed Combat'});
 const names=new Set(c.rows('weapons','weapon').filter(w=>value(w,'type').toLowerCase()==='melee').map(w=>value(w,'useskill')||(/blade/i.test(value(w,'category'))?'Blades':value(w,'category')==='Clubs'?'Clubs':value(w,'category')==='Unarmed'?'Unarmed Combat':'')));
 for(const name of names){if(!name||name==='Unarmed Combat')continue;const n=skill(name);if(n!==null)rows.push({name:'Parry · '+name,pool:Math.max(0,reaction+n+wounds),formula:'REA + '+name},{name:'Full parry · '+name,pool:Math.max(0,reaction+2*n+wounds),formula:'REA + '+name+' × 2'});}
 return rows;
}
