import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {resolve,extname,sep} from 'node:path';
import {chromium} from 'playwright';
const root=resolve('Chummer.PWA');
const server=createServer(async(req,res)=>{try{const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);let file=resolve(root,'.'+pathname);if(!file.startsWith(root+sep))throw Error('Invalid path');if(pathname.endsWith('/'))file=resolve(file,'index.html');const bytes=await readFile(file);res.setHeader('Content-Type',({'.html':'text/html','.mjs':'text/javascript','.js':'text/javascript','.css':'text/css','.pdf':'application/pdf','.json':'application/json','.webmanifest':'application/manifest+json'}[extname(file)]||'application/octet-stream'));res.end(bytes);}catch{res.writeHead(404);res.end();}});
await new Promise(done=>server.listen(0,'127.0.0.1',done));
const browser=await chromium.launch();
try{
 const page=await browser.newPage();page.on('pageerror',error=>console.error('Browser error:',error.message));
 for(const [path,success] of [['/tests/','ALL TESTS PASSED'],['/tests/printing.html','ALL PRINT CHECKS PASSED']]){
  const response=await page.goto(`http://127.0.0.1:${server.address().port}${path}`);if(!response.ok())throw Error('Test page returned '+response.status());
  await page.waitForFunction(()=>/ALL .* PASSED|\nFAILED$/.test(document.querySelector('pre')?.textContent||''),{},{timeout:120000});
  const result=await page.locator('pre').innerText();console.log(result);if(!result.includes(success)||result.includes('FAIL '))throw Error('Browser verification failed');
 }
 // Assert public exports and persisted manual edits are reachable through the real UI.
 await page.goto(`http://127.0.0.1:${server.address().port}/dist/`);
 await page.getByRole('button',{name:'Try sample',exact:true}).click();
 await page.getByLabel('Essence',{exact:true}).fill('5.75');
 await page.getByRole('button',{name:'Combat',exact:true}).click();
 await page.getByRole('button',{name:'Physical damage 1',exact:true}).waitFor();
 await page.reload();if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='5.75')throw Error('Autosave failed');
 await page.getByRole('button',{name:'Export',exact:true}).click();
 for(const name of ['Print full character','Print official character sheet'])if(!await page.getByRole('button',{name,exact:true}).isVisible())throw Error('Missing '+name);
 console.log('PASS real UI autosave and export options');
 await page.getByRole('button',{name:'Close export',exact:true}).click();
 const {demo}=await import('../dist/core.mjs');
 const sample=new TextDecoder().decode(demo),icon=(await readFile('Chummer.PWA/dist/assets/icon.png')).toString('base64');
 const portraitSample=sample.replace('</character>','<mugshot>'+icon+'</mugshot></character>');
 let expanded=false,treeReads=0;
 await page.route('https://api.github.com/**',async route=>{
  const path=new URL(route.request().url()).pathname;let json;
  if(path==='/repos/test/private')json={id:99,private:true,permissions:{push:true},default_branch:'main'};
  else if(path.includes('/git/trees/')){treeReads++;json={tree:[{path:'Nightjar.chum',type:'blob',mode:'100644',sha:'portrait'},...(expanded?[{path:'Archive/Willow.chum',type:'blob',mode:'100644',sha:'second'}]:[])]};}
  else{const first=path.endsWith('/portrait')||path.endsWith('/Nightjar.chum');const xml=first?portraitSample:sample.replace('<alias>Nightjar</alias>','<alias>Willow test</alias>');json={sha:first?'portrait':'second',encoding:'base64',size:xml.length,content:Buffer.from(xml).toString('base64')};}
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
 if(await page.getByLabel('Runner',{exact:true}).inputValue()!=='Willow test')throw Error('Wrong repository character opened');
 const local=await page.getByRole('option',{name:'Nightjar',exact:true}).getAttribute('value');
 await page.getByLabel('Character',{exact:true}).selectOption(local);
 await page.waitForFunction(()=>document.querySelector('input[aria-label="Runner"]')?.value==='Nightjar');
 if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='5.75')throw Error('Opening repository character overwrote local edits');
 console.log('PASS Open refreshes all characters, shows portrait, searches folders and preserves local edits');

}finally{await browser.close();await new Promise(done=>server.close(done));}
