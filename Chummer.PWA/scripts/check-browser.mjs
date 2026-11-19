import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {resolve,extname,sep} from 'node:path';
import {chromium} from 'playwright';
const root=resolve('Chummer.PWA'),dist=resolve(process.env.PWA_TEST_DIST||'Chummer.PWA/dist');
const server=createServer(async(req,res)=>{try{const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);const base=pathname.startsWith('/dist/')?dist:root;let file=resolve(base,pathname.startsWith('/dist/')?'./'+pathname.slice(6):'.'+pathname);if(!file.startsWith(base+sep)&&file!==base)throw Error('Invalid path');if(pathname.endsWith('/'))file=resolve(file,'index.html');const bytes=await readFile(file);res.setHeader('Content-Type',({'.html':'text/html','.mjs':'text/javascript','.js':'text/javascript','.css':'text/css','.pdf':'application/pdf','.json':'application/json','.webmanifest':'application/manifest+json'}[extname(file)]||'application/octet-stream'));res.end(bytes);}catch{res.writeHead(404);res.end();}});
await new Promise(done=>server.listen(0,'127.0.0.1',done));
const browser=await chromium.launch();
try{
 const page=await browser.newPage();page.on('pageerror',error=>console.error('Browser error:',error.message));
 for(const [path,success] of [['/tests/','ALL TESTS PASSED'],['/tests/printing.html','ALL PRINT CHECKS PASSED']]){
  const response=await page.goto(`http://127.0.0.1:${server.address().port}${path}`);if(!response.ok())throw Error('Test page returned '+response.status());
  await page.waitForFunction(()=>/ALL .* PASSED|\nFAILED$/.test(document.querySelector('pre')?.textContent||''),{},{timeout:120000});
  const result=await page.locator('pre').innerText();console.log(result);if(!result.includes(success)||result.includes('FAIL '))throw Error('Browser verification failed');
 }
 // Simulate an older unversioned script remaining in the browser HTTP cache.
 if(process.env.PWA_TEST_DIST)await page.route('**/dist/*.mjs',route=>route.fulfill({contentType:'text/javascript',body:'throw new Error("Stale unversioned module")'}));
 await page.clock.install();
 // Assert public exports and persisted manual edits are reachable through the real UI.
 await page.goto(`http://127.0.0.1:${server.address().port}/dist/`);
 if(!await page.getByRole('button',{name:'Install app',exact:true}).isVisible())throw Error('Missing initial install button');
 await page.getByRole('button',{name:'Try sample',exact:true}).click();
 const skillHeader=page.getByRole('group',{name:'Skill filters',exact:true});
 if(await skillHeader.getByRole('button',{name:'Active',exact:true}).getAttribute('aria-pressed')!=='true'||await skillHeader.getByRole('button',{name:'Pool > 0',exact:true}).getAttribute('aria-pressed')!=='true')throw Error('Wrong skill defaults');
 await skillHeader.getByRole('button',{name:'Combat',exact:true}).click();
 if(await page.getByLabel('Perception rating',{exact:true}).count())throw Error('Combat includes physical skills');
 await skillHeader.getByRole('button',{name:'Knowledge',exact:true}).click();
 if(await page.getByLabel('Pistols rating',{exact:true}).count())throw Error('Knowledge includes combat');
 await skillHeader.getByRole('button',{name:'Active',exact:true}).click();
 for(const width of [390,820]){await page.setViewportSize({width,height:1000});const fits=await skillHeader.evaluate(node=>node.scrollWidth<=node.clientWidth+1);if(!fits)throw Error('Skill filters overflow at '+width);}
 await page.setViewportSize({width:1280,height:900});
 console.log('PASS skill header defaults, category controls and phone/tablet fit');

 await page.getByLabel('Essence',{exact:true}).fill('5.75');
 await page.getByRole('navigation',{name:'Dossier sections'}).getByRole('button',{name:'Combat',exact:true}).click();
 await page.getByRole('button',{name:'Physical damage 1',exact:true}).waitFor();
 await page.getByRole('button',{name:'Refresh app',exact:true}).click();await page.waitForURL(/refresh=/);if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='5.75')throw Error('Autosave failed');
 await page.getByRole('button',{name:'Export',exact:true}).click();
 for(const name of ['Print full character','Print official character sheet'])if(!await page.getByRole('button',{name,exact:true}).isVisible())throw Error('Missing '+name);
 console.log('PASS Refresh app preserves pending edits and export options');
 await page.getByRole('button',{name:'Close export',exact:true}).click();
 const {demo}=await import('../dist/core.mjs');
 const sample=new TextDecoder().decode(demo).replace('</skills>','<skill><name>Japanese</name><knowledge>True</knowledge><skillcategory>Language</skillcategory><attribute>INT</attribute><rating>0</rating><totalvalue>0</totalvalue></skill></skills>'),icon=(await readFile('Chummer.PWA/dist/assets/icon.png')).toString('base64');
 const portraitSample=sample.replace('</character>','<mugshot>'+icon+'</mugshot></character>');
 let expanded=false,treeReads=0,writes=0;
 const remoteFiles=new Map([['Nightjar.chum',{xml:portraitSample,sha:'portrait'}],['Archive/Willow.chum',{xml:sample.replace('<alias>Nightjar</alias>','<alias>Willow test</alias>').replace('</character>','<magician>True</magician></character>'),sha:'second'}]]);
 const blobs=new Map([...remoteFiles.values()].map(v=>[v.sha,v.xml]));
 await page.route('https://api.github.com/**',async route=>{
  const request=route.request(),path=decodeURIComponent(new URL(request.url()).pathname);let json;
  if(path==='/repos/test/private')json={id:99,private:true,permissions:{push:true},default_branch:'main'};
  else if(path.includes('/git/trees/')){treeReads++;json={tree:[...remoteFiles.entries()].filter(([name])=>expanded||!name.startsWith('Archive/')).map(([name,file])=>({path:name,type:'blob',mode:'100644',sha:file.sha}))};}
  else if(path.includes('/git/blobs/')){const sha=path.split('/').at(-1),xml=blobs.get(sha);if(!xml)return route.fulfill({status:404,json:{}});json={sha,encoding:'base64',size:xml.length,content:Buffer.from(xml).toString('base64')};}
  else if(path.includes('/contents/')){const key=path.split('/contents/')[1],existing=remoteFiles.get(key);
   if(request.method()==='PUT'){const body=request.postDataJSON();if((existing?.sha||undefined)!==body.sha)return route.fulfill({status:409,json:{}});const xml=Buffer.from(body.content,'base64').toString(),sha='saved-'+(++writes);remoteFiles.set(key,{xml,sha});blobs.set(sha,xml);json={content:{sha}};}
   else{if(!existing)return route.fulfill({status:404,json:{}});json={sha:existing.sha,encoding:'base64',size:existing.xml.length,content:Buffer.from(existing.xml).toString('base64')};}
  }else return route.fulfill({status:404,json:{}});
  await route.fulfill({json});
 });
 await page.getByRole('button',{name:'Open',exact:true}).click();
 await page.getByLabel('New or existing repository',{exact:true}).fill('test/private');
 await page.getByLabel('Token',{exact:true}).fill('synthetic-test-token');
 await page.getByRole('button',{name:'Connect',exact:true}).click();
 await page.locator('.character-choice img').waitFor();
 if(!await page.getByRole('dialog').getByText('Nightjar',{exact:true}).isVisible())throw Error('Missing character alias');
 await page.getByRole('button',{name:'Close character browser',exact:true}).click();
 expanded=true;
 await page.getByRole('button',{name:'Open',exact:true}).click();
 await page.locator('.character-choice').nth(1).waitFor();
 if(treeReads<2)throw Error('Open did not refresh repository');
 await page.getByLabel('Find a repository character',{exact:true}).fill('archive');
 await page.locator('.character-choice').click();
 await page.locator('#open-repository').waitFor({state:'hidden'});
 if(await page.getByLabel('Runner',{exact:true}).inputValue()!=='Willow test')throw Error('Wrong repository character opened');
 await page.getByRole('button',{name:'Magic',exact:true}).click();
 await page.getByRole('button',{name:'Pool > 0',exact:true}).click();
 await page.getByLabel('Astral Combat rating',{exact:true}).waitFor();
 await page.setViewportSize({width:390,height:1000});if(!await page.getByLabel('Skill filters',{exact:true}).evaluate(node=>node.scrollWidth<=node.clientWidth+1))throw Error('Magic filters overflow phone');
 await page.getByRole('button',{name:'Pool > 0',exact:true}).click();await page.getByRole('button',{name:'Active',exact:true}).click();await page.setViewportSize({width:1280,height:900});

 const local=await page.getByRole('option',{name:'Nightjar',exact:true}).getAttribute('value');
 await page.getByLabel('Character',{exact:true}).selectOption(local);
 await page.waitForFunction(()=>document.querySelector('input[aria-label="Runner"]')?.value==='Nightjar');
 if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='5.75')throw Error('Opening repository character overwrote local edits');
 console.log('PASS Open refreshes all characters, shows portrait, searches folders and preserves local edits');
 const countBefore=await page.locator('#characters option').count();
 await page.getByRole('button',{name:'Open',exact:true}).click();
 await page.getByLabel('Find a repository character',{exact:true}).fill('archive');
 await page.locator('.character-choice').click();await page.locator('#open-repository').waitFor({state:'hidden'});
 await page.getByRole('group',{name:'Skill filters'}).getByRole('button',{name:'Knowledge',exact:true}).click();
 if(await page.getByLabel('Japanese rating',{exact:true}).inputValue()!=='N')throw Error('Native language is not N');
 await page.getByRole('group',{name:'Skill filters'}).getByRole('button',{name:'Active',exact:true}).click();
 await page.getByLabel('Essence',{exact:true}).fill('4.25');
 await page.getByLabel('Character',{exact:true}).selectOption(local);
 await page.waitForFunction(()=>document.querySelector('input[aria-label="Runner"]')?.value==='Nightjar');
 await page.getByRole('button',{name:'Open',exact:true}).click();
 await page.getByLabel('Find a repository character',{exact:true}).fill('archive');
 await page.locator('.character-choice').click();await page.locator('#open-repository').waitFor({state:'hidden'});
 if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='4.25')throw Error('Reopening overwrote unsynced edits');
 await page.getByRole('button',{name:'Close',exact:true}).click();
 await page.getByRole('button',{name:'Install app',exact:true}).waitFor();
 await page.reload();await page.getByRole('button',{name:'Resume Willow test',exact:true}).click();
 await page.waitForFunction(()=>document.querySelector('input[aria-label="Runner"]')?.value==='Willow test');
 if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='4.25')throw Error('Close lost pending edits');
 if(await page.locator('#characters option').count()!==countBefore)throw Error('Repeated Open duplicated a character');
 console.log('PASS install entry, close saves pending edits, closed state survives reload and reopening avoids duplicates');


 // Reconnect after reload; connection tokens are intentionally memory-only.
 await page.getByRole('button',{name:'GitHub',exact:true}).click();
 await page.getByLabel('Token',{exact:true}).fill('synthetic-test-token');
 await page.getByRole('button',{name:'Connect',exact:true}).click();
 await page.locator('.character-choice').first().waitFor();
 await page.getByRole('button',{name:'Close character browser',exact:true}).click();
 await page.waitForFunction(()=>document.querySelector('#status')?.textContent.includes('GitHub up to date'));
 await page.context().setOffline(true);
 await page.getByLabel('Essence',{exact:true}).fill('4.5');
 await page.getByRole('navigation',{name:'Dossier sections'}).getByRole('button',{name:'Combat',exact:true}).click();await page.getByRole('button',{name:'Physical damage 1',exact:true}).waitFor();
 const writesOffline=writes;await page.clock.fastForward(60000);if(writes!==writesOffline)throw Error('Autosync wrote while offline');
 await page.context().setOffline(false);
 await page.waitForFunction(()=>document.querySelector('#status')?.textContent.includes('GitHub up to date'));
 if(!remoteFiles.get('Archive/Willow.chum').xml.includes('<totaless>4.5</totaless>'))throw Error('Online catch-up lost offline edit');
 await page.clock.fastForward(60000);if(writes!==writesOffline+1)throw Error('Clean minute tick uploaded again');
 console.log('PASS minute autosync skips unchanged data and catches up immediately after offline edits');
 function externalEdit(path,sha){const file=remoteFiles.get(path);const xml=file.xml.replace(/<karma>.*?<\/karma>/,'<karma>99</karma>');remoteFiles.set(path,{xml,sha});blobs.set(sha,xml);}
 async function localEditAndConflict(path,essence,sha){externalEdit(path,sha);await page.getByRole('button',{name:'Character',exact:true}).click();await page.getByLabel('Essence',{exact:true}).fill(essence);await page.getByRole('navigation',{name:'Dossier sections'}).getByRole('button',{name:'Combat',exact:true}).click();await page.getByRole('button',{name:'Physical damage 1',exact:true}).waitFor();await page.clock.fastForward(60000);await page.locator('#sync-conflict').waitFor();}
 await localEditAndConflict('Archive/Willow.chum','4.75','external-1');
 await page.getByLabel('New character name',{exact:true}).fill('Willow backup');
 await page.getByRole('button',{name:'Save as new name',exact:true}).click();await page.locator('#sync-conflict').waitFor({state:'hidden'});
 await page.waitForFunction(()=>document.querySelector('#status')?.textContent.includes('GitHub up to date'));
 const backup=[...remoteFiles.keys()].find(key=>key.includes('Willow backup-'));if(!backup||remoteFiles.get('Archive/Willow.chum').sha!=='external-1')throw Error('Save-as conflict overwrote original');
 await localEditAndConflict(backup,'4.9','external-2');await page.getByRole('button',{name:'Overwrite GitHub',exact:true}).click();await page.locator('#sync-conflict').waitFor({state:'hidden'});
 if(!remoteFiles.get(backup).xml.includes('<totaless>4.9</totaless>'))throw Error('Explicit overwrite failed');
 await localEditAndConflict(backup,'5','external-3');const beforeDelete=writes;
 await page.getByRole('button',{name:'Delete local copy',exact:true}).click();await page.locator('#delete-local-confirm').getByRole('button',{name:'Delete local copy',exact:true}).click();await page.locator('#sync-conflict').waitFor({state:'hidden'});
 if(remoteFiles.get(backup).sha!=='external-3'||writes!==beforeDelete)throw Error('Delete local changed GitHub');
 if(await page.getByRole('button',{name:'Resume Willow backup',exact:true}).count())throw Error('Deleted local copy remains');
 console.log('PASS conflict save-as preserves original, explicit overwrite saves local data, Delete local copy leaves GitHub untouched');
 if(process.env.PWA_TEST_DIST){
  await page.evaluate(()=>navigator.serviceWorker.ready.then(()=>true));
  await page.reload();await page.getByRole('button',{name:'Open',exact:true}).waitFor();
  await page.context().setOffline(true);
  await page.reload();await page.getByRole('button',{name:'Open',exact:true}).click();
  await page.locator('#repository').waitFor();
  if(await page.locator('header').evaluate(node=>getComputedStyle(node).display)!=='flex')throw Error('Offline stylesheet missing');
  await page.context().setOffline(false);
  console.log('PASS versioned assets bypass stale scripts and reload offline with styles and working Open');
 }


}finally{await browser.close();await new Promise(done=>server.close(done));}
