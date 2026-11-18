import {value,children,child,truth,splitNotes} from './core.mjs';
export const citation=node=>[value(node,'source'),value(node,'page')&&'p. '+value(node,'page')].filter(Boolean).join(' ');
export const named=node=>value(node,'name')+(value(node,'extra')?' ('+value(node,'extra')+')':'');
export const native=skill=>skill.rating===0&&value(skill.node,'skillcategory')==='Language'&&truth(value(skill.node,'knowledge'));
export function printSkills(c){return c.skills.filter(s=>s.rating>0||s.pool>0||native(s)).sort((a,b)=>(Number(b.rating>0||native(b))-Number(a.rating>0||native(a)))||a.name.localeCompare(b.name));}
const details=(node,keys)=>keys.map(key=>value(node,key)?key+': '+value(node,key):'').filter(Boolean).join(' · ');
export function printSections(c){
 const out=[];const add=(title,columns,rows,weights)=>{if(rows.length)out.push({title,columns,rows,weights:weights||columns.map(()=>1)});};
 add('Personal data',['Field','Value'],['name','alias','metatype','age','sex','height','weight','karma','totalkarma','streetcred','notoriety','nuyen'].map(key=>[key,c.get(key)]),[1,4]);
 add('Attributes',['Attribute','Base','Total'],c.attributes.filter(a=>!['ESS','INI'].includes(a.id)&&(a.id!=='MAG'||c.magician||c.adept)&&(a.id!=='RES'||c.technomancer)).map(a=>[a.id,a.base,a.total]).concat([['ESS','',c.essence]]),[3,1,1]);
 add('Initiative',['Type','Initiative','Passes'],[['Physical',c.initiative,c.passes],['Matrix',c.matrix,c.matrixPasses],...(c.magician?[['Astral',c.astral,3]]:[])],[3,1,1]);
 add('Condition monitors',['Track','Damage / Capacity','Wounds'],[true,false].map(p=>[p?'Physical':'Stun',`${c.damage(p)} / ${c.capacity(p)}`,c.ignoresWounds(p)?'Ignored':`Every ${c.woundThreshold} boxes; offset ${c.woundOffset}`]).concat([['Combined penalty',c.wounds,'Included in pools and initiative']]),[2,2,4]);
 add('Skills',['Skill / Specialization','Rating','Attribute','Pool','Source'],printSkills(c).map(s=>[named(s.node)+(value(s.node,'spec')?' ('+value(s.node,'spec')+')':'')+(native(s)?' (Native)':''),native(s)?'N':s.rating,s.attribute,native(s)?'N':s.pool,citation(s.node)]),[5,1,1,1,2]);
 add('Weapons',['Weapon','DV / AP / Mode','Ammunition','Source'],c.rows('weapons','weapon').map(n=>[named(n),[c.weaponDamage(n),'AP '+value(n,'ap'),value(n,'mode')].filter(Boolean).join(' · '),['ammo','ammoremaining','ammoremaining2','ammoremaining3','ammoremaining4'].map(k=>value(n,k)?k+': '+value(n,k):'').filter(Boolean).join('; '),citation(n)]),[3,3,3,1.5]);
 const generic=(title,collection,item,keys)=>add(title,['Name','Details','Source'],c.rows(collection,item).map(n=>[named(n),details(n,keys),citation(n)]),[3,5,1.5]);
 generic('Armor','armors','armor',['b','i','notes']);generic('Qualities','qualities','quality',['qualitytype','notes']);generic('Lifestyles','lifestyles','lifestyle',['baselifestyle','notes']);
 const augment=[];function walk(nodes,depth=0){for(const node of nodes){augment.push(['  '.repeat(depth)+named(node),details(node,['rating','ess','grade','notes']),citation(node)]);const nested=child(node,'children');if(nested)walk(children(nested,'cyberware'),depth+1);}}walk(c.rows('cyberwares','cyberware'));add('Augmentations',['Name','Details','Source'],augment,[3,5,1.5]);
 const programs=new Set(c.programs.map(r=>r.node));add('Equipment / Commlinks',['Name','Details','Source'],c.gear.filter(r=>!programs.has(r.node)&&value(r.node,'category')!=='Program Options').map(({node,depth})=>['  '.repeat(depth)+named(node),details(node,['rating','qty','response','signal','system','firewall','notes']),citation(node)]),[3,5,1.5]);
 add('Programs',['Program / Upgrades','Rating','Loaded','Source'],c.programs.map(({node})=>{const nested=child(node,'children');return [named(node)+(nested?' · '+children(nested,'gear').map(n=>named(n)+' '+value(n,'rating')).join(', '):''),value(node,'rating'),truth(value(node,'equipped'))?'Yes':'No',citation(node)];}),[6,1,1,2]);
 if(c.magician)generic('Spells','spells','spell',['type','range','duration','dv','notes']);if(c.adept)generic('Adept powers','powers','power',['rating','notes']);if(c.technomancer)generic('Complex forms','complexforms','complexform',['rating','notes']);if(c.magician||c.technomancer)generic(c.technomancer?'Sprites':'Spirits','spirits','spirit',['force','services','bound','notes']);generic('Vehicles / Drones','vehicles','vehicle',['handling','accel','speed','body','armor','pilot','sensor','notes']);
 add('Contacts',['Contact','Connection','Loyalty','Notes'],c.rows('contacts','contact').map(n=>[named(n),value(n,'connection'),value(n,'loyalty'),splitNotes(value(n,'notes')).plain]),[2,1,1,5]);
 add('Personal history',['Field','Details'],Object.entries(c.metadata?.fields||{}).filter(([key,text])=>key.startsWith('history.')&&text).map(([key,text])=>[key.slice(8),text]),[2,5]);
 add('Unlisted equipment notes',['Name','Details'],(c.metadata?.pendingEquipment||[]).map(n=>[n.name,[n.category,...Object.entries(n.stats||{}).map(([k,v])=>k+': '+v),...Object.entries(n.adjustments||{}).map(([k,v])=>k+': '+v)].join(' · ')]),[2,5]);
 for(const [title,text] of [['IDs / Licenses',c.field('identities')],['Description',c.get('description')],['Background',c.get('background')],['Concept',c.get('concept')],['Notes',c.plainNotes]])if(text)add(title,[''],[[text]]);
 return out;
}

export function primaryArmor(c){
 const all=c.rows('armors','armor');
 const worn=all.filter(n=>truth(value(n,'equipped')));
 const score=n=>Number.isFinite(Number(value(n,'b')))&&Number.isFinite(Number(value(n,'i')))?Number(value(n,'b'))+Number(value(n,'i')):-Infinity;
 return (worn.length?worn:all).map((node,index)=>({node,index})).sort((a,b)=>score(b.node)-score(a.node)||(Number(value(b.node,'b'))||0)-(Number(value(a.node,'b'))||0)||a.index-b.index)[0]?.node;
}
