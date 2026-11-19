export function installGuide(nav=navigator){
 const apple=/iPad|iPhone|iPod/.test(nav.userAgent)||(nav.platform==='MacIntel'&&nav.maxTouchPoints>1);
 return apple?['Open this page in Safari.','Tap Share (or the page menu, then Share).','Choose Add to Home Screen. Enable Open as Web App if offered, then tap Add.']:['Open this page in Chrome, Edge, or another browser that supports installed web apps.','Open the browser menu and choose Install app, Install this site as an app, or Add to Home screen.','Confirm to add SR 4 Dossier to your Home screen or desktop.'];
}
export function installController(win=window,nav=navigator,onChange=()=>{}){
 let pending,accepted=false;const mode=win.matchMedia('(display-mode: standalone)');
 const installed=()=>accepted||nav.standalone===true||mode.matches;
 win.addEventListener('beforeinstallprompt',event=>{event.preventDefault();pending=event;onChange();});
 win.addEventListener('appinstalled',()=>{pending=undefined;accepted=true;onChange();});
 mode.addEventListener?.('change',onChange);
 return {installed,async request(){
  if(installed())return {kind:'installed'};
  if(!pending)return {kind:'guide',steps:installGuide(nav)};
  const event=pending;pending=undefined;await event.prompt();const choice=await event.userChoice;onChange();
  return {kind:choice?.outcome==='accepted'?'accepted':'dismissed'};
 }};
}
