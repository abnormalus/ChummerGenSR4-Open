import {value,truth} from './core.mjs?v=0e271b8e6690afea33581da5b3145f8f861b1434';
const KNOWLEDGE=new Set(['Academic','Interest','Interests','Language','Professional','Street']);
export function knowledgeSkill(skill){return truth(value(skill.node,'knowledge'))||KNOWLEDGE.has(value(skill.node,'skillcategory'));}
export const magicSkillsApply=character=>character.magician||character.adept;
export function filteredSkills(character,category='Active',positive=true){
 if(category==='Magic'&&!magicSkillsApply(character))category='Active';
 return character.skills.filter(skill=>{
  if(positive&&skill.pool<=0)return false;
  const knowledge=knowledgeSkill(skill),saved=value(skill.node,'skillcategory');
  if(category==='Knowledge')return knowledge;
  if(knowledge)return false;
  if(category==='Combat')return saved==='Combat Active';
  if(category==='Magic')return saved==='Magical Active';
  return true;
 }).sort((a,b)=>a.name.localeCompare(b.name));
}
