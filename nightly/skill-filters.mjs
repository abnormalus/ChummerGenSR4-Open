import {value,truth} from './core.mjs?v=0c86bbc158b285dfc1db19bb775a5e82e672c6e1';
const KNOWLEDGE=new Set(['Academic','Interest','Interests','Language','Professional','Street']);
export function knowledgeSkill(skill){return truth(value(skill.node,'knowledge'))||KNOWLEDGE.has(value(skill.node,'skillcategory'));}
export const nativeLanguage=skill=>skill.rating===0&&value(skill.node,'skillcategory')==='Language';
export const skillRating=skill=>nativeLanguage(skill)?'N':skill.rating;
export const magicSkillsApply=character=>character.magician||character.adept;
export function filteredSkills(character,category='Active',positive=true){
 if(category==='Magic'&&!magicSkillsApply(character))category='Active';
 return character.skills.filter(skill=>{
  if(positive&&skill.pool<=0&&!(category==='Knowledge'&&nativeLanguage(skill)))return false;
  const knowledge=knowledgeSkill(skill),saved=value(skill.node,'skillcategory');
  if(category==='Knowledge')return knowledge;
  if(knowledge)return false;
  if(category==='Combat')return saved==='Combat Active';
  if(category==='Magic')return saved==='Magical Active';
  return true;
 }).sort((a,b)=>a.name.localeCompare(b.name));
}

export const perceptionSense=skill=>skill.name.match(/^Perception \(([^)]+)\)$/)?.[1]||'';
export function perceptionDifferences(character,base){return character.skills.filter(s=>perceptionSense(s)&&s.pool!==base.pool).map(s=>({name:perceptionSense(s),pool:s.pool}));}
