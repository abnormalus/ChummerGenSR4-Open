import {value as xmlValue,child,children,integer,truth} from './core.mjs?v=065be32a5d73de7f4067f71beefc2819397dcd41';
import {ranges} from './weapon-ranges.mjs?v=065be32a5d73de7f4067f71beefc2819397dcd41';

const value=(node,key)=>node?xmlValue(node,key):'';
const groups={Archery:['Bows','Crossbows'],Automatics:['Assault Rifles','Machine Pistols','Submachine Guns','Battle Rifles'],Blades:['Blades','Cyberware Blades'],Clubs:['Clubs'],'Heavy Weapons':['Assault Cannons','Grenade Launchers','Missile Launchers','Mortar Launchers','Light Machine Guns','Medium Machine Guns','Heavy Machine Guns'],Longarms:['Shotguns','Sniper Rifles','Sports Rifles'],'Throwing Weapons':['Throwing Weapons','Cyberware Throwing Weapons'],'Unarmed Combat':['Unarmed','Cyberware Clubs','Cyberware']};
export function attachments(weapon){return ['accessories','weaponmods'].flatMap(key=>[...(child(weapon,key)?.children||[])]);}
const installed=node=>truth(value(node,'installed'));
export function loadedAmmo(character,weapon,slot=1){const guid=value(weapon,'ammoloaded'+(slot===1?'':slot)).toLowerCase();return guid&&guid!=='00000000-0000-0000-0000-000000000000'?character.gear.find(({node})=>value(node,'guid').toLowerCase()===guid)?.node:undefined;}
export function activeSlot(character,weapon){const id=value(weapon,'guid')||'row-'+character.rows('weapons','weapon').indexOf(weapon);const n=integer(character.field(`weapon.${id}.ammoSlot`),1);return n>=1&&n<=4?n:1;}
const numericBonus=node=>value(node,'dicepool')==='Rating'?integer(value(node,'rating')):value(node,'dicepool')==='-Rating'?-integer(value(node,'rating')):integer(value(node,'dicepool'));
export function weaponPool(character,weapon){
 let category=value(weapon,'category'),spec='';if(category==='Special Weapons'&&value(weapon,'range'))category=value(weapon,'range');
 let name=value(weapon,'useskill')||Object.keys(groups).find(key=>groups[key].includes(category))||'Pistols';
 if(!value(weapon,'useskill')&&(/Exotic/.test(category)||/^Cyberware (Hold-Outs|Light Pistols|Machine Pistols|Heavy Pistols|Submachine Guns|Shotguns|Grenade Launchers|Tasers)$/.test(category)))name=category.includes('Melee')?'Exotic Melee Weapon':'Exotic Ranged Weapon';
 if(['Flamethrowers','Laser Weapons'].includes(category)&&!value(weapon,'useskill')){name='Exotic Ranged Weapon';spec=category;}
 if(name.includes('Exotic')&&!spec)spec=value(weapon,'name');
 // Synthetic/imported weapons occasionally omit category; never call unarmed Pistols.
 if(!value(weapon,'useskill')&&!category&&value(weapon,'name').startsWith('Unarmed Attack'))name='Unarmed Combat';
 const skill=character.skills.find(s=>s.name===name&&!truth(value(s.node,'knowledge'))&&(!spec||value(s.node,'spec')===spec));
 if(!skill)return {text:'—',description:name+' · Skill not saved'};
 const mods=attachments(weapon).filter(installed),ammo=loadedAmmo(character,weapon,activeSlot(character,weapon));
 const smart=mods.some(n=>value(n,'name').startsWith('Smartgun System'))&&['Automatics','Exotic Ranged Weapon','Heavy Weapons','Longarms','Pistols'].includes(name)&&character.improvements.some(n=>value(n,'improvementttype')==='Smartlink')?2:0;
 const bonus=mods.reduce((n,m)=>n+numericBonus(m),0)+smart+integer(value(child(ammo||weapon,'weaponbonus'),'pool'));
 const total=Math.max(0,skill.pool+bonus),specialization=value(skill.node,'spec');
 const specialized=!name.includes('Exotic')&&specialization&&[value(weapon,'name'),value(weapon,'weaponname'),category,value(weapon,'spec'),value(weapon,'spec2')].includes(specialization);
 return {text:String(total)+(specialized?` (${total+2})`:''),description:`${name} ${skill.pool}${bonus?' · Weapon modifiers '+(bonus>0?'+':'')+bonus:''}${specialized?' · Specialization '+specialization+' +2 (in parentheses)':''}`};
}
export function weaponRanges(character,weapon){
 const category=value(weapon,'range')||value(weapon,'category'),data=ranges[category];if(!data)return [];
 const slot=activeSlot(character,weapon),ammo=loadedAmmo(character,weapon,slot);
 const bonus=(100+children(child(weapon,'weaponmods')||weapon,'weaponmod').reduce((n,m)=>n+integer(value(m,'rangebonus')),0)+integer(value(child(ammo||weapon,'weaponbonus'),'range')))/100;
 const multiplier=Number(value(weapon,'rangemultiply')||1);if(!Number.isFinite(multiplier)||multiplier<0||bonus<0)return [];
 const throwing=value(weapon,'category')==='Throwing Weapons'||value(weapon,'useskill')==='Throwing Weapons';
 function distance(expression){const match=String(expression).match(/^(STR|BOD|\d+)(?:\*(\d+))?$/);if(!match)return null;const base=match[1]==='STR'?character.total('STR')+(throwing?character.improvement('ThrowRange'):0):match[1]==='BOD'?character.total('BOD'):Number(match[1]);return Math.ceil(Math.floor(base*Number(match[2]||1)*multiplier)*bonus);}
 const limits=['min','short','medium','long','extreme'].map(k=>distance(data[k]));if(limits.some(n=>n===null))return [];
 return ['Short','Medium','Long','Extreme'].map((label,i)=>({label,text:`${limits[i]+(i?1:0)}–${limits[i+1]} m`}));
}
export function conditionPenalty(character,physical,n){if(character.ignoresWounds(physical))return 0;const current=Math.floor(Math.max(0,n-character.woundOffset)/character.woundThreshold),prior=Math.floor(Math.max(0,n-1-character.woundOffset)/character.woundThreshold);return current>prior?-current:0;}
