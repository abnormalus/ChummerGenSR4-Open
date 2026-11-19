import {PDFDocument,rgb} from './vendor/pdf-lib.mjs?v=065be32a5d73de7f4067f71beefc2819397dcd41';
import fontkit from './vendor/fontkit.mjs?v=065be32a5d73de7f4067f71beefc2819397dcd41';
import {value,child,children,truth} from './core.mjs?v=065be32a5d73de7f4067f71beefc2819397dcd41';
import {printSections,printSkills,native,named,primaryArmor} from './print-data.mjs?v=065be32a5d73de7f4067f71beefc2819397dcd41';
const BLACK=rgb(0,0,0),GREY=rgb(.89,.92,.91),MUTED=rgb(.3,.35,.34);
const text=value=>String(value??'').replace(/\t/g,'    ').replace(/[\u0000-\u0008\u000b\u000c\u000e-\u001f]/g,'');
async function asset(path){const r=await fetch(new URL(path,import.meta.url));if(!r.ok)throw Error('Printing resource unavailable. Open the app online once to cache its print resources.');return new Uint8Array(await r.arrayBuffer());}
function wrap(raw,font,size,width){
 const lines=[];for(const paragraph of text(raw).split(/\r?\n/)){let line='';for(const word of paragraph.split(/\s+/)){if(!word)continue;const candidate=line?line+' '+word:word;if(font.widthOfTextAtSize(candidate,size)<=width){line=candidate;continue;}if(line){lines.push(line);line='';}let part='';for(const char of word){if(part&&font.widthOfTextAtSize(part+char,size)>width){lines.push(part);part='';}part+=char;}line=part;}lines.push(line);}return lines.length?lines:[''];
}
function appendix(doc,font,title,sections){
 let page,y=0,pageNumber=doc.getPageCount();
 function begin(){page=doc.addPage([612,792]);pageNumber++;page.drawText(text(title),{x:36,y:756,size:13,font,color:BLACK,maxWidth:540});page.drawText('SR 4 Dossier · '+pageNumber,{x:36,y:24,size:8,font,color:MUTED});y=726;}
 function heading(section,continued=false){if(!page||y<100)begin();page.drawRectangle({x:36,y:y-21,width:540,height:24,color:GREY});page.drawText(section.title+(continued?' (continued)':''),{x:43,y:y-14,size:10,font,color:BLACK});y-=28;}
 for(const section of sections){heading(section);const sum=section.weights.reduce((a,b)=>a+b,0),widths=section.weights.map(w=>540*w/sum);
  function columns(){if(!section.columns.some(Boolean))return;let x=40;section.columns.forEach((name,i)=>{page.drawText(name,{x,y:y-9,size:7,font,color:MUTED});x+=widths[i];});y-=17;}
  columns();for(let index=0;index<section.rows.length;index++){
   const cells=section.rows[index].map((cell,i)=>wrap(cell,font,8,widths[i]-10));let offset=0,total=Math.max(...cells.map(lines=>lines.length));
   while(offset<total){if(y<64){begin();heading(section,true);columns();}const count=Math.max(1,Math.min(total-offset,Math.floor((y-43)/11))),height=count*11+8;
    if(index%2===0)page.drawRectangle({x:36,y:y-height,width:540,height,color:rgb(.96,.97,.96)});
    let x=41;cells.forEach((lines,i)=>{lines.slice(offset,offset+count).forEach((line,j)=>page.drawText(line,{x,y:y-11-j*11,size:8,font,color:BLACK}));x+=widths[i];});
    page.drawLine({start:{x:36,y:y-height},end:{x:576,y:y-height},thickness:.3,color:GREY});y-=height;offset+=count;if(offset<total){begin();heading(section,true);columns();}
   }
  }y-=14;
 }
}
export async function characterPDF(c,{official=false,templateBytes,fontBytes}={}){
 c.bytes(true);
 const doc=official?await PDFDocument.load(templateBytes||await asset('./assets/SR4A-Character-Sheet.pdf')):await PDFDocument.create();
 doc.registerFontkit(fontkit);const font=await doc.embedFont(fontBytes||await asset('./assets/DejaVuSans.ttf'),{subset:true});
 doc.setTitle(c.title+(official?' - Official SR4A character sheet':' - Full character'));doc.setCreator('SR 4 Dossier');
 const sections=printSections(c);
 if(!official){appendix(doc,font,c.title,sections);return doc.save();}
 if(doc.getPageCount()!==2)throw Error('The embedded official form must contain exactly two pages.');
 const expanded=[];let page=doc.getPage(0);
 function put(raw,x,y,width,label,size=8,forceFit=false){let shown=text(raw);if(!shown)return;let s=size;while(s>(forceFit?4:6)&&font.widthOfTextAtSize(shown.replace(/\n/g,' '),s)>width)s-=.25;
  if(shown.includes('\n')||font.widthOfTextAtSize(shown,s)>width){if(forceFit){shown=shown.replace(/\s+/g,' ');s=Math.min(s,width/font.widthOfTextAtSize(shown,1));}else{expanded.push([`*${expanded.length+1} ${label}`,shown]);shown='*'+expanded.length;s=6;}}
  page.drawText(shown,{x,y:792-y-s,size:s,font,color:BLACK});
 }
 function table(rows,x,y,widths,limit,title){rows.slice(0,limit).forEach((row,i)=>row.forEach((v,j)=>{if(j<x.length)put(v,x[j],y+i*14,widths[j],title+' / '+(row[0]||'')+' / '+(j+1),7);}));}
 const rows=(collection,item)=>c.rows(collection,item),weapons=rows('weapons','weapon'),ranged=weapons.filter(n=>value(n,'type').toLowerCase()==='ranged'),melee=weapons.filter(n=>value(n,'type').toLowerCase()!=='ranged');
 const ammo=n=>{const slot=value(n,'activeammoslot')||'1',remaining=value(n,'ammoremaining'+(slot==='1'?'':slot));return remaining?remaining+'/'+value(n,'ammo'):value(n,'ammo');};
 const formSkills=printSkills(c);
 put([...new Set([c.get('name'),c.get('alias')].filter(Boolean))].join(' / '),124,78,191,'Name / Alias');put(c.get('metatype'),87,94,72,'Metatype');put(c.field('ethnicity'),199,94,116,'Ethnicity');
 for(const [key,x,w] of [['age',63,37],['sex',123,50],['height',204,31],['weight',266,49],['totalkarma',91,23],['karma',164,27],['streetcred',235,24],['notoriety',297,18]])put(c.get(key),x,['age','sex','height','weight'].includes(key)?110:126,w,key);
 ['BOD','AGI','REA','STR','CHA','INT','LOG','WIL'].forEach((key,i)=>{const a=c.attributes.find(a=>a.id===key);put(a?a.base===a.total?a.total:`${a.base} (${a.total})`:'0',104,195+i*14,39,key);});
 [c.total('EDG'),Math.max(0,c.total('EDG')-Number(c.get('edgeused')||0)),c.essence,c.technomancer?c.total('RES'):c.magician||c.adept?c.total('MAG'):'-',c.initiative,c.passes,`${c.matrix} / ${c.matrixPasses} IP`,c.magician?c.astral:'-'].forEach((v,i)=>put(v,227,195+i*14,38,'Attribute / Initiative '+(i+1)));
 const armor=primaryArmor(c);if(armor){put(value(armor,'name'),416,78,86,'Armor');put(value(armor,'b'),514,74,19,'Ballistic');put(value(armor,'i'),552,74,18,'Impact');}
 if(ranged[0]){const n=ranged[0];put(value(n,'name'),447,94,122,'Primary ranged');[c.weaponDamage(n),value(n,'ap'),value(n,'mode'),value(n,'rc'),ammo(n)].forEach((v,i)=>put(v,[384,427,466,501,548][i],109,[25,17,21,17,22][i],'Primary ranged details',7));}
 const ranked=melee.map((n,i)=>({n,i,dv:Number(c.weaponDamage(n).match(/^\d+(?:\.\d+)?/)?.[0]??-Infinity)})).sort((a,b)=>b.dv-a.dv||a.i-b.i);if(ranked[0]){const n=ranked[0].n;put(value(n,'name'),447,126,122,'Primary melee');put(value(n,'reach'),389,141,48,'Reach');put(c.weaponDamage(n),468,141,47,'Melee damage');put(value(n,'ap'),545,141,23,'Melee AP');}
 for(const physical of [true,false]){const capacity=c.capacity(physical),damage=c.damage(physical),count=physical?18:12,origin=physical?365.5:481.2;for(let i=0;i<count;i++){const x=origin+i%3*31.4+1,top=222.8+Math.floor(i/3)*30.2;if(i>=Math.max(capacity,damage))page.drawRectangle({x,y:792-top-28.2,width:29.4,height:28.2,color:rgb(.22,.22,.22)});else if(i<damage)put('X',x+7,top+3,20,'Damage',19);}if(Math.max(capacity,damage)>count)expanded.push([physical?'Physical monitor':'Stun monitor',`${damage} damage / ${capacity} boxes`]);}
 formSkills.slice(0,34).forEach((s,i)=>{const x=i<17?51.4:188.7,y=389+i%17*14;put(named(s.node)+(value(s.node,'spec')?' ('+value(s.node,'spec')+')':'')+(native(s)?' (Native)':''),x,y,60,'Skill',7);put(native(s)?'N':s.rating,x+63,y,13,'Rating',7);put(c.total(s.attribute),x+79,y,12,'Attribute',7);put(native(s)?'N':s.pool,x+94,y,15,'Pool',7);page.drawEllipse({x:x+(truth(value(s.node,'knowledge'))?125:115),y:792-y-4.5,xScale:4,yScale:5.5,borderWidth:.5,borderColor:BLACK});});
 table(rows('qualities','quality').map(n=>[named(n),value(n,'notes')]),[361.5,469.5],492,[104,86],9,'Quality');rows('qualities','quality').slice(0,9).forEach((n,i)=>page.drawEllipse({x:(value(n,'qualitytype').toLowerCase().includes('negative')?568:558)+4,y:792-(492+i*14)-4.5,xScale:4,yScale:5.5,borderWidth:.5,borderColor:BLACK}));
 const lifestyle=rows('lifestyles','lifestyle')[0];if(lifestyle)put(value(lifestyle,'name')||value(lifestyle,'baselifestyle'),117,676,449,'Lifestyle');put(c.get('nuyen'),77,692,123,'Nuyen');const ids=c.gear.filter(r=>value(r.node,'category')==='ID/Credsticks').map(r=>named(r.node)+' '+value(r.node,'rating'));put([c.field('identities'),...ids].filter(Boolean).join('; '),48,725,518,'IDs / Licenses');
 put('* Expanded entries and full character details follow on continuation pages.',57,764,520,'Continuation',6);
 page=doc.getPage(1);
 table(ranged.map(n=>[value(n,'name'),c.weaponDamage(n),value(n,'ap'),value(n,'mode'),value(n,'rc'),ammo(n)]),[57.4,122.7,162.5,200.4,252.6,290.9],89,[62,37,35,49,35,28],6,'Ranged weapon');
 table(melee.map(n=>[value(n,'name'),c.weaponDamage(n),value(n,'ap'),value(n,'reach')]),[57.4,159.7,224.5,291.7],230,[99,61,64,27],7,'Melee weapon');
 table(rows('cyberwares','cyberware').map(n=>[named(n),value(n,'rating'),value(n,'notes'),value(n,'ess')]),[57.4,156,193,289],382,[96,34,92,30],8,'Augmentation');
 if(c.magician)table(rows('spells','spell').map(n=>['name','type','range','duration','dv'].map(k=>value(n,k))),[57.1,131.7,174.1,224.6,278.3],553,[71,39,47,50,40],6,'Spell');
 if(c.magician||c.technomancer)table(rows('spirits','spirit').map(n=>[value(n,'name'),value(n,'force'),value(n,'services'),truth(value(n,'bound'))?(c.technomancer?'Registered':'Bound'):(c.technomancer?'Unregistered':'Unbound')]),[57.6,130.9,172,232],688,[70,37,56,87],5,'Spirit / Sprite');
 table(rows('armors','armor').map(n=>[named(n),value(n,'b')+' / '+value(n,'i'),value(n,'notes')]),[365.6,437.6,537.2],90,[69,96,43],5,'Armor');
 const links=c.gear.filter(r=>value(r.node,'category')==='Commlink'),link=(links.filter(r=>truth(value(r.node,'active'))).at(-1)||links[0])?.node;
 if(link){const nested=child(link,'children'),os=nested&&children(nested,'gear').find(n=>value(n,'category')==='Commlink Operating System');put(value(link,'name'),408,204,72,'Commlink');put(os?value(os,'name'):'',499,204,80,'OS');put(c.matrixResponse,409,218,57,'Response');put(os?value(os,'system'):value(link,'system'),505,218,74,'System');put(value(link,'signal'),392,232,73,'Signal');put(os?value(os,'firewall'):value(link,'firewall'),507,232,72,'Firewall');}
 put(c.programs.map(({node})=>named(node)+' '+value(node,'rating')+(truth(value(node,'equipped'))?' [L]':'')).join('; '),409,246,170,'Programs');
 const vehicle=rows('vehicles','vehicle')[0];if(vehicle)for(const [key,x,y,w] of [['name',401,350,62],['handling',507,350,72],['accel',394,364,78],['speed',504,364,75],['pilot',387,378,84],['body',503,378,76],['armor',397,392,74],['sensor',507,392,72],['notes',393,406,186]])put(value(vehicle,key),x,y,w,'Vehicle '+key,8,key==='name');
 table(rows('contacts','contact').map(n=>[value(n,'name'),value(n,'loyalty'),value(n,'connection')]),[365.6,490.3,530.6],488,[121,37,49],9,'Contact');
 const abilities=[...(c.adept?rows('powers','power').map(n=>[named(n),value(n,'rating')]):[]),...(c.technomancer?rows('complexforms','complexform').map(n=>[named(n),value(n,'rating')]):[])];table(abilities,[365.6,544.7],669,[176,35],6,'Power / Form');
 put('* Expanded entries and full character details follow on continuation pages.',57,764,520,'Continuation',6);
 // Include a complete readable snapshot after the original form, retaining every list entry and citation.
 appendix(doc,font,c.title+' - Continuation',[...(expanded.length?[{title:'Expanded form entries',columns:['Field','Full value'],weights:[2,5],rows:expanded}]:[]),...sections]);
 return doc.save();
}
