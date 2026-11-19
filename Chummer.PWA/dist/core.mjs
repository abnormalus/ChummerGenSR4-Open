export const MAX_BYTES = 32 * 1024 * 1024;
const encoder = new TextEncoder();
export const integer = (value, fallback = 0) => /^-?\d+$/.test(String(value).trim()) && Number.isSafeInteger(Number(value)) ? Number(value) : fallback;
export const truth = value => String(value).toLowerCase() === 'true';
export const children = (node, name) => [...node.children].filter(child => child.tagName === name);
export const child = (node, name) => children(node, name)[0];
export const value = (node, name) => child(node, name)?.textContent ?? '';
export function setValue(node, name, text) {
  let target = child(node, name);
  if (!target) { target = node.ownerDocument.createElement(name); node.append(target); }
  target.textContent = String(text);
}
export function base64(bytes) {
  let text = '';
  for (let i = 0; i < bytes.length; i += 6144) text += btoa(String.fromCharCode(...bytes.subarray(i, i + 6144)));
  return text;
}
export function unbase64(text) { return Uint8Array.from(atob(text.replace(/\s/g, '')), char => char.charCodeAt(0)); }
export async function fingerprint(bytes) {
  return [...new Uint8Array(await crypto.subtle.digest('SHA-256', bytes))].map(byte => byte.toString(16).padStart(2, '0')).join('');
}
export function decodeXML(bytes) {
  if (bytes.length > MAX_BYTES) throw new Error('This character exceeds the 32 MB working-copy limit.');
  const encoding = bytes[0] === 0xff && bytes[1] === 0xfe ? 'utf-16le' : bytes[0] === 0xfe && bytes[1] === 0xff ? 'utf-16be' : 'utf-8';
  return new TextDecoder(encoding, { fatal: true }).decode(bytes);
}
const START = '[CHUMMER-DOSSIER-V1]', END = '[/CHUMMER-DOSSIER-V1]';
export function splitNotes(text) {
  const start = text.indexOf(START);
  if (start < 0) return { plain: text, metadata: null, envelope: '' };
  const end = text.indexOf(END, start + START.length);
  if (end < 0 || text.indexOf(START, start + START.length) >= 0) throw new Error('Unreadable dossier notes. The original file has been retained.');
  let metadata;
  try { metadata = JSON.parse(new TextDecoder().decode(unbase64(text.slice(start + START.length, end).trim()))); } catch { throw new Error('Unreadable dossier notes.'); }
  if (metadata.version !== 1) throw new Error('This dossier notes version is not supported yet.');
  return { plain: text.slice(0, start) + text.slice(end + END.length), metadata, envelope: text.slice(start, end + END.length) };
}
function arithmetic(input) {
  if (input.length > 256) return null;
  const source = input.replace(/\s/g, ''); let index = 0;
  function factor() {
    if (source[index] === '-') { index++; return -factor(); }
    if (source[index] === '+') { index++; return factor(); }
    if (source[index] === '(') { index++; const n = expression(); if (source[index++] !== ')') throw 0; return n; }
    if (source.slice(index, index + 4) === 'min(') { index += 4; const a = expression(); if (source[index++] !== ',') throw 0; const b = expression(); if (source[index++] !== ')') throw 0; return Math.min(a, b); }
    const match = source.slice(index).match(/^(?:\d+(?:\.\d*)?|\.\d+)/); if (!match) throw 0; index += match[0].length; return Number(match[0]);
  }
  function term() { let n = factor(); while ('*/'.includes(source[index] || '\0')) { const op = source[index++], rhs = factor(); n = op === '*' ? n * rhs : n / rhs; } return n; }
  function expression() { let n = term(); while ('+-'.includes(source[index] || '\0')) { const op = source[index++], rhs = term(); n = op === '+' ? n + rhs : n - rhs; } return n; }
  try { const n = expression(); return index === source.length && Number.isFinite(n) && Math.abs(n) < 1000000 ? n : null; } catch { return null; }
}
export class Character {
  constructor(bytes) {
    this.original = new Uint8Array(bytes); this.changed = false; this.revision = 0;
    const text = decodeXML(this.original);
    if (/<!DOCTYPE|<!ENTITY/i.test(text)) throw new Error('Document types and XML entities are not supported.');
    this.document = new DOMParser().parseFromString(text, 'application/xml'); this.root = this.document.documentElement;
    if (this.document.querySelector('parsererror') || this.root.tagName !== 'character' || !child(this.root, 'attributes') || !['', 'SR4'].includes(this.get('gameedition'))) throw new Error('Choose an SR4 Chummer character (.chum), not a print export.');
    splitNotes(this.get('notes'));
    for (const contact of this.rows('contacts', 'contact')) splitNotes(value(contact, 'notes'));
  }
  get(key) { return value(this.root, key); }
  set(key, text) { if (this.get(key) === String(text)) return; setValue(this.root, key, text); this.touch(); }
  touch() { this.changed = true; this.revision++; }
  setNode(node, key, text) { if (value(node, key) === String(text)) return; setValue(node, key, text); this.touch(); }
  get title() { return this.get('alias') || this.get('name') || 'Unnamed runner'; }
  get metadata() { return splitNotes(this.get('notes')).metadata; }
  field(key) { return this.metadata?.fields?.[key] ?? ''; }
  setField(key, text) {
    const notes = splitNotes(this.get('notes'));
    const data = notes.metadata || { version: 1, fields: {}, poolModifiers: {}, pendingEquipment: [], contactDetails: {} };
    data.fields ||= {}; data.fields[key] = String(text);
    this.set('notes', notes.plain + START + '\n' + base64(encoder.encode(JSON.stringify(data))) + '\n' + END);
  }
  get plainNotes() { return splitNotes(this.get('notes')).plain; }
  setPlainNotes(text) { const notes = splitNotes(this.get('notes')); this.set('notes', text + notes.envelope); }
  rows(collection, item) { const container = child(this.root, collection); return container ? children(container, item) : []; }
  get attributes() { return this.rows('attributes', 'attribute').map(node => ({ node, id: value(node, 'name'), base: integer(value(node, 'value')), total: integer(value(node, 'totalvalue'), integer(value(node, 'value'))) })); }
  total(id) { return this.attributes.find(attribute => attribute.id === id)?.total ?? 0; }
  get essence() { return this.get('totaless') || '6'; }
  setAttribute(id, key, text) {
    const attribute = this.attributes.find(attribute => attribute.id === id); if (!attribute) return;
    const priorSkills = this.skills;
    const parsed = integer(text, NaN);
    if (!Number.isInteger(parsed)) { this.setNode(attribute.node, key === 'base' ? 'value' : 'totalvalue', text); return; }
    const delta = parsed - attribute[key];
    this.setNode(attribute.node, key === 'base' ? 'value' : 'totalvalue', parsed);
    if (key === 'base') this.setNode(attribute.node, 'totalvalue', attribute.total + delta);
    for (const skill of priorSkills.filter(skill => skill.attribute === id)) this.setSkillPool(skill.index, skill.pool + delta);
  }
  get magician() { return truth(this.get('magician')); }
  get adept() { return truth(this.get('adept')); }
  get technomancer() { return truth(this.get('resenabled')) || truth(this.get('technomancerenabled')); }
  get improvements() { return this.rows('improvements', 'improvement').filter(node => !/^false$/i.test(value(node, 'enabled'))); }
  improvement(type) {
    const rows = this.improvements.filter(node => value(node, 'improvementttype') === type && !truth(value(node, 'addtorating')) && !(this.technomancer && type === 'MatrixInitiativePass' && value(node, 'improvementsource') === 'Gear'));
    const sum = entries => {
      let result = 0; const unique = new Map();
      for (const node of entries) { const key = value(node, 'unique'), n = integer(value(node, 'val')); if (!key) result += n; else unique.set(key, [...(unique.get(key) || []), n]); }
      if (unique.has('precedence0')) return Math.max(...unique.get('precedence0'));
      if (unique.has('precedence1')) return unique.get('precedence1').reduce((a, b) => a + b, 0);
      return result + [...unique.values()].reduce((n, values) => n + Math.max(...values), 0);
    };
    return sum(rows.filter(node => !truth(value(node, 'custom')))) + sum(rows.filter(node => truth(value(node, 'custom'))));
  }
  damage(physical) { return Math.max(0, integer(this.get(physical ? 'physicalcmfilled' : 'stuncmfilled'))); }
  capacity(physical) { return Math.max(0, 8 + Math.ceil(this.total(physical ? 'BOD' : 'WIL') / 2) + this.improvement(physical ? 'PhysicalCM' : 'StunCM')); }
  ignoresWounds(physical) { return this.improvements.some(node => value(node, 'improvementttype') === (physical ? 'IgnoreCMPenaltyPhysical' : 'IgnoreCMPenaltyStun')); }
  get woundThreshold() { return Math.max(1, 3 + this.improvement('CMThreshold')); }
  get woundOffset() { return this.improvement('CMThresholdOffset'); }
  get wounds() { return [true, false].reduce((n, physical) => n - (this.ignoresWounds(physical) ? 0 : Math.floor(Math.max(0, Math.min(this.damage(physical), this.capacity(physical)) - this.woundOffset) / this.woundThreshold)), 0); }
  get importedWounds() { return this.improvements.filter(node => value(node, 'improvementsource') === 'ConditionMonitor').reduce((n, node) => n + integer(value(node, 'val')), 0); }
  get skills() {
    const wounds = this.wounds, imported = this.importedWounds;
    return this.rows('skills', 'skill').map((node, index) => {
      const rating = integer(value(node, 'rating')), attribute = value(node, 'attribute'), fallback = this.total(attribute) + (rating === 0 && truth(value(node, 'default')) ? -1 : rating);
      return { node, index, name: value(node, 'name'), attribute, rating, pool: Math.max(0, child(node, 'totalvalue') ? integer(value(node, 'totalvalue'), fallback) - imported + wounds : fallback + wounds) };
    });
  }
  setSkillPool(index, pool) { this.setNode(this.rows('skills', 'skill')[index], 'totalvalue', integer(pool) - this.wounds + this.importedWounds); }
  setSkillRating(index, rating) {
    const prior = this.skills[index], n = integer(rating, NaN);
    this.setNode(prior.node, 'rating', rating);
    if (Number.isInteger(n)) this.setSkillPool(index, prior.pool + n - prior.rating);
  }
  get gear() {
    const flatten = (nodes, path = []) => nodes.flatMap((node, index) => {
      const at = [...path, index], nested = child(node, 'children');
      return [{ node, path: at.join('.'), depth: path.length }, ...flatten(nested ? children(nested, 'gear') : [], at)];
    });
    return flatten(this.rows('gears', 'gear'));
  }
  get programs() { return this.gear.filter(({ node }) => ['Matrix Programs', 'Sensor Software'].includes(value(node, 'category'))).sort((a, b) => value(a.node, 'name').localeCompare(value(b.node, 'name'))); }
  get matrixResponse() {
    if (/^-?\d+$/.test(this.field('matrixResponse'))) return integer(this.field('matrixResponse'));
    const links = this.rows('gears', 'gear').filter(node => value(node, 'category') === 'Commlink' && truth(value(node, 'active'))), link = links.at(-1);
    if (!link) return 0;
    const upgrades = this.gear.filter(({ node }) => node.parentElement?.parentElement === link && value(node, 'category') === 'Commlink Upgrade');
    return Math.max(integer(value(link, 'response')), ...upgrades.map(({ node }) => integer(value(node, 'response')))) + (truth(value(link, 'homenode')) ? Math.ceil(this.total('INT') / 2) : 0);
  }
  get initiative() { return Math.max(0, this.total('REA') + this.total('INT') + this.improvement('Initiative') + this.wounds + integer(this.field('initiativeModifier'))); }
  get passes() { return integer(this.field('initiativePasses'), 1 + this.improvement('InitiativePass') + this.improvement('InitiativePassAdd')); }
  get astral() { return Math.max(0, this.total('INT') * 2 + this.wounds + integer(this.field('astralModifier'))); }
  get matrix() {
    if (/Sprite$|A\.I\.$/.test(this.get('metatype')) || ['Technocritters', 'Protosapients'].includes(this.get('metatypecategory'))) return this.field('matrixInitiativeOverride') || '—';
    return Math.max(0, (this.technomancer ? this.total('INT') * 2 + 1 + this.improvement('LivingPersonaResponse') : this.total('INT') + this.matrixResponse + this.improvement('MatrixInitiative')) + this.wounds + integer(this.field('matrixInitiativeModifier')));
  }
  get matrixPasses() { return (this.technomancer ? 3 : 1) + this.improvement('MatrixInitiativePass') + this.improvement('MatrixInitiativePassAdd') + integer(this.field('matrixPassesModifier')); }
  weaponDamage(node) {
    const guid = value(node, 'guid') || 'row-' + this.rows('weapons', 'weapon').indexOf(node), override = this.field(`weapon.${guid}.damage`);
    if (override) return override;
    const raw = value(node, 'damage'), match = raw.match(/^(.*)(P or S|P|S)(.*)$/); if (!match) return raw;
    const expression = match[1].replace(/STR|AGI|BOD|REA|INT|LOG|WIL|CHA|MAG|RES/g, key => this.total(key));
    const result = arithmetic(expression); if (result === null) return raw;
    let damage = Math.ceil(result), type = match[2];
    if (value(node, 'name') === 'Unarmed Attack') { damage += this.improvement('UnarmedDV'); if (this.improvements.some(row => value(row, 'improvementttype') === 'UnarmedDVPhysical')) type = 'P'; }
    return damage + type + match[3];
  }
  bytes(validate = false) {
    if (validate) {
      const check = (text, label) => { if (!/^-?\d+$/.test(text) || Number(text) < -2147483648 || Number(text) > 2147483647) throw new Error(label + ' must be a whole number before exporting or syncing.'); };
      for (const key of ['karma', 'totalkarma', 'streetcred', 'notoriety', 'nuyen', 'physicalcmfilled', 'stuncmfilled']) if (child(this.root, key)) check(this.get(key), key);
      for (const { node, id } of this.attributes) { check(value(node, 'value'), id); if (child(node,'totalvalue')) check(value(node,'totalvalue'), id+' total'); }
      for (const skill of this.skills) check(value(skill.node, 'rating'), skill.name + ' rating');
      for (const contact of this.rows('contacts', 'contact')) for (const key of ['connection', 'loyalty']) if (child(contact, key)) check(value(contact, key), key);
      if (this.get('totaless') && !/^-?(?:\d+(?:\.\d*)?|\.\d+)$/.test(this.get('totaless'))) throw new Error('Essence must be a number.');
    }
    if (!this.changed) return new Uint8Array(this.original);
    return encoder.encode('<?xml version="1.0" encoding="utf-8"?>\n' + new XMLSerializer().serializeToString(this.document).replace(/^<\?xml[^?]*\?>\s*/, ''));
  }
}
export const demo = encoder.encode(`<?xml version="1.0"?><character><name>Morgan Vale</name><alias>Nightjar</alias><metatype>Human</metatype><age>28</age><sex>Nonbinary</sex><nuyen>2400</nuyen><karma>8</karma><totalkarma>36</totalkarma><attributes>${[['BOD',4],['AGI',5],['REA',4],['STR',3],['CHA',3],['INT',4],['LOG',3],['WIL',4],['EDG',3]].map(([id,n])=>`<attribute><name>${id}</name><value>${n}</value><totalvalue>${n}</totalvalue></attribute>`).join('')}</attributes><totaless>6</totaless><physicalcmfilled>0</physicalcmfilled><stuncmfilled>0</stuncmfilled><skills><skill><name>Pistols</name><skillcategory>Combat Active</skillcategory><attribute>AGI</attribute><rating>4</rating><totalvalue>9</totalvalue></skill><skill><name>Perception</name><skillcategory>Physical Active</skillcategory><attribute>INT</attribute><rating>3</rating><totalvalue>7</totalvalue></skill><skill><name>Negotiation</name><skillcategory>Social Active</skillcategory><attribute>CHA</attribute><rating>2</rating><totalvalue>5</totalvalue></skill><skill><name>Astral Combat</name><skillcategory>Magical Active</skillcategory><attribute>WIL</attribute><rating>0</rating><totalvalue>0</totalvalue></skill></skills><weapons><weapon><name>Ares Predator IV</name><category>Heavy Pistols</category><type>Ranged</type><damage>5P</damage><ap>-1</ap><mode>SA</mode><ammo>15(c)</ammo><ammoremaining>15</ammoremaining><source>SR4</source><page>307</page></weapon><weapon><name>Unarmed Attack</name><type>Melee</type><damage>(STR/2)S</damage><ap>-</ap></weapon></weapons><gears><gear><name>Browse</name><category>Matrix Programs</category><rating>3</rating><equipped>True</equipped></gear></gears><contacts><contact><name>Patch</name><connection>3</connection><loyalty>2</loyalty><notes>A street doc who keeps unusual hours.</notes></contact></contacts><background>A former courier with a good memory for bad streets. Synthetic sample character; edit freely.</background><notes>Keep an escape route open.</notes></character>`);
