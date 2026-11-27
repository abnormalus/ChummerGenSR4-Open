import {value,child,children,integer,truth,arithmetic} from './core.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
import {grades} from './augmentation-grades.mjs?v=4c727c878c1d270ca3834bb301bb9a4a277953d6';
export function augmentationEssence(c,node){
 if(node.parentElement?.tagName==='children'&&value(node,'capacity').includes('['))return 'Included in parent';
 const raw=value(node,'ess'),rating=integer(value(node,'rating'));let expression=raw;
 const fixed=raw.match(/^FixedValues\((.*)\)$/);if(fixed)expression=fixed[1].split(',')[rating-1]||'';
 const base=arithmetic(expression.replaceAll('Rating',rating));if(base===null)return raw||'Not saved';
 const type=value(node,'improvementsource')||'Cyberware',grade=grades[type]?.[value(node,'grade')||'Standard'];if(grade===undefined)return raw+' · '+value(node,'grade');
 let multiplier=grade-(truth(value(node,'suite')) ? 0.1 : 0)-integer(value(node,'essdiscount'))/100;
 const reduction=type=>c.improvements.filter(n=>value(n,'improvementttype')===type).reduce((sum,n)=>sum+1-integer(value(n,'val'))/100,0);
 if(!/^Genetech|^Genetic Infusions/.test(value(node,'category'))){multiplier-=reduction(type==='Bioware'?'BiowareEssCost':'CyberwareEssCost');if(type==='Bioware'&&value(node,'category')==='Basic')multiplier-=reduction('BasicBiowareEssCost');}
 let cost=base*multiplier;if(type==='Cyberware'&&c.improvements.some(n=>value(n,'improvementttype')==='SensitiveSystem'))cost*=2;
 const decimals=Math.min(6,Math.max(0,integer(c.get('essencedecimals'),2)));return String(Number(cost.toFixed(decimals)));
}
export function augmentationEffects(c,node){
 const ids=new Set([node,...node.querySelectorAll('cyberware, gear')].map(n=>value(n,'guid')).filter(Boolean));const rows=[];
 const labels={Attribute:'Attribute',Skill:'Skill',SkillGroup:'Skill group',SkillCategory:'Skill category',SkillAttribute:'Attribute-linked skills',Initiative:'Initiative',InitiativePass:'Initiative passes',InitiativePassAdd:'Extra initiative passes',BallisticArmor:'Ballistic armor',ImpactArmor:'Impact armor',DamageResistance:'Damage resistance',Smartlink:'Smartlink',UnarmedDV:'Unarmed damage',UnarmedAP:'Unarmed AP',CMThreshold:'Wound threshold',CMThresholdOffset:'Ignored damage boxes',PhysicalCM:'Physical condition boxes',StunCM:'Stun condition boxes',IgnoreCMPenaltyPhysical:'Ignores Physical wound penalties',IgnoreCMPenaltyStun:'Ignores Stun wound penalties'};
 for(const effect of c.improvements.filter(n=>ids.has(value(n,'sourcename')))){
  const type=value(effect,'improvementttype'),target=value(effect,'improvedname'),n=integer(value(effect,type==='Attribute'?'aug':'val')),amount=n?(n>0?'+':'')+n:'';
  const name=target?target+(type==='Attribute'?'':' · '+(labels[type]||type.replace(/([a-z])([A-Z])/g,'$1 $2'))):labels[type]||type.replace(/([a-z])([A-Z])/g,'$1 $2');
  rows.push({name,amount:amount||'Enabled'});
 }
 // Saved bonuses without generated improvements still expose the stat/condition.
 if(!rows.length){const bonus=child(node,'bonus');if(bonus)for(const item of bonus.children){if(/^select/i.test(item.tagName))continue;const target=value(item,'name')||value(item,'skill'),text=value(item,'val')||value(item,'bonus')||(!item.children.length?item.textContent:'');const resolved=arithmetic(text.replaceAll('Rating',integer(value(node,'rating'))));rows.push({name:target||item.tagName.replace(/([a-z])([A-Z])/g,'$1 $2'),amount:resolved===null?(text||'Enabled'):(resolved>0?'+':'')+resolved});}}
 return rows;
}
export const augmentationChildren=node=>children(child(node,'children')||node,'cyberware');
