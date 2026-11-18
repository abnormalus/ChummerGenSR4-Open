import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {resolve,extname,sep} from 'node:path';
import {chromium} from 'playwright';
const root=resolve('Chummer.PWA');
const server=createServer(async(req,res)=>{try{const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);let file=resolve(root,'.'+pathname);if(!file.startsWith(root+sep))throw Error('Invalid path');if(pathname.endsWith('/'))file+='index.html';const bytes=await readFile(file);res.setHeader('Content-Type',({'.html':'text/html','.mjs':'text/javascript','.js':'text/javascript','.css':'text/css','.pdf':'application/pdf','.json':'application/json','.webmanifest':'application/manifest+json'}[extname(file)]||'application/octet-stream'));res.end(bytes);}catch{res.writeHead(404);res.end();}});
await new Promise(done=>server.listen(0,'127.0.0.1',done));
const browser=await chromium.launch();
try{
 const page=await browser.newPage();
 for(const [path,success] of [['/tests/','ALL TESTS PASSED'],['/tests/printing.html','ALL PRINT CHECKS PASSED']]){
  await page.goto(`http://127.0.0.1:${server.address().port}${path}`);
  await page.waitForFunction(()=>/ALL .* PASSED|\nFAILED$/.test(document.querySelector('pre')?.textContent||''),{},{timeout:120000});
  const result=await page.locator('pre').innerText();console.log(result);if(!result.includes(success)||result.includes('FAIL '))throw Error('Browser verification failed');
 }
 // Assert public exports and persisted manual edits are reachable through the real UI.
 await page.goto(`http://127.0.0.1:${server.address().port}/dist/`);
 await page.getByRole('button',{name:'Try sample',exact:true}).click();
 await page.getByLabel('Essence',{exact:true}).fill('5.75');
 await page.getByRole('button',{name:'Combat',exact:true}).click();
 await page.reload();if(await page.getByLabel('Essence',{exact:true}).inputValue()!=='5.75')throw Error('Autosave failed');
 await page.getByRole('button',{name:'Export',exact:true}).click();
 for(const name of ['Print full character','Print official character sheet'])if(!await page.getByRole('button',{name,exact:true}).isVisible())throw Error('Missing '+name);
 console.log('PASS real UI autosave and export options');
}finally{await browser.close();await new Promise(done=>server.close(done));}
