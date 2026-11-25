import {Character,child,children,value,unbase64} from './core.mjs?v=f0c22eb4658216f295bf3dba670c3788820008d1';
// Only embedded raster images are accepted. Character XML cannot supply a remote image URL.
export function portraitBytes(character){
 const index=Number(character.get('mainmugshotindex')||0),container=child(character.root,'mugshots');
 const images=container?children(container,'mugshot'):[];
 const encoded=(images[index]?.textContent||images[0]?.textContent||character.get('mugshot')).replace(/\s/g,'');
 if(!encoded||encoded.length>4*1024*1024)return null;
 let bytes;try{bytes=unbase64(encoded);}catch{return null;}
 let type;if(bytes[0]===137&&bytes[1]===80&&bytes[2]===78&&bytes[3]===71)type='image/png';
 else if(bytes[0]===255&&bytes[1]===216&&bytes[2]===255)type='image/jpeg';
 else if(String.fromCharCode(...bytes.slice(0,6)).match(/^GIF8[79]a$/))type='image/gif';
 else if(String.fromCharCode(...bytes.slice(0,4))==='RIFF'&&String.fromCharCode(...bytes.slice(8,12))==='WEBP')type='image/webp';
 return type?{bytes,type}:null;
}
export async function characterPreview(bytes){
 const c=new Character(bytes),portrait=portraitBytes(c);let image;
 if(portrait){try{
  const bitmap=await createImageBitmap(new Blob([portrait.bytes],{type:portrait.type}),{resizeWidth:96,resizeQuality:'high'});
  const canvas=document.createElement('canvas');canvas.width=96;canvas.height=96;const scale=Math.max(96/bitmap.width,96/bitmap.height);canvas.getContext('2d').drawImage(bitmap,(96-bitmap.width*scale)/2,(96-bitmap.height*scale)/2,bitmap.width*scale,bitmap.height*scale);bitmap.close();
  const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/png'));if(blob)image=URL.createObjectURL(blob);
 }catch{/* A broken portrait must not prevent opening the character. */}}
 return {title:c.title,name:c.get('name'),metatype:c.get('metatype'),image};
}
export function matchesCharacter(item,query){return [item.name,item.path,item.preview?.title,item.preview?.name,item.preview?.metatype].filter(Boolean).join(' ').toLocaleLowerCase().includes(query.trim().toLocaleLowerCase());}
