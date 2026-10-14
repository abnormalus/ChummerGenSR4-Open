// Locally bundled Mozilla PDF.js; no network/CDN or PDF scripting required.
import * as pdfjs from './legacy/build/pdf.mjs';
const $ = id => document.getElementById(id);
const status = $('status');
const report = message => { try { window.invokeCSharpAction?.(message); } catch {} };
function fail() {
  status.textContent = 'This PDF could not be opened. Check that it is a valid PDF and try linking it again in Chummer Settings.';
  report('pdf-error');
}
try {
  const { EventBus, PDFViewer, PDFLinkService, PDFFindController } = await import('./legacy/web/pdf_viewer.mjs');
  pdfjs.GlobalWorkerOptions.workerSrc = './legacy/build/pdf.worker.mjs';
  const eventBus = new EventBus();
  const linkService = new PDFLinkService({ eventBus });
  linkService.externalLinkEnabled = false;
  const findController = new PDFFindController({ eventBus, linkService });
  const viewer = new PDFViewer({ container: $('container'), viewer: $('viewer'), eventBus, linkService,
    findController, imageResourcesPath: './legacy/web/images/', annotationMode: 1, maxCanvasPixels: 16777216 });
  linkService.setViewer(viewer);
  const initialPage = Math.max(1, parseInt(new URLSearchParams(location.hash.slice(1)).get('page'), 10) || 1);
  const toolbar = document.querySelector('header');
  function layout() { $('container').style.top = `${toolbar.offsetHeight + (status.textContent ? status.offsetHeight : 0)}px`; }
  new ResizeObserver(layout).observe(toolbar);
  eventBus.on('pagesinit', () => {
    viewer.currentScaleValue = 'page-width';
    viewer.currentPageNumber = Math.min(initialPage, viewer.pagesCount);
    $('pages').textContent = `/ ${viewer.pagesCount}`;
    $('page').max = viewer.pagesCount;
    status.textContent = initialPage > viewer.pagesCount ? 'The requested page is past the end of this PDF. Check the sourcebook page offset in Settings.' : '';
    layout(); report('pdf-ready');
  });
  eventBus.on('pagechanging', e => { $('page').value = e.pageNumber; });
  eventBus.on('scalechanging', e => {
    if (e.presetValue && [...$('zoom').options].some(option => option.value === e.presetValue)) {
      $('zoom').value = e.presetValue;
    } else {
      let custom = $('zoom').querySelector('[data-custom]');
      if (!custom) { custom = document.createElement('option'); custom.dataset.custom = 'true'; $('zoom').append(custom); }
      custom.value = String(e.scale); custom.textContent = `${Math.round(e.scale * 100)}%`; $('zoom').value = custom.value;
    }
  });
  eventBus.on('updatefindmatchescount', e => { $('matches').textContent = `${e.matchesCount.current} / ${e.matchesCount.total}`; });
  eventBus.on('updatefindcontrolstate', e => { if (e.state === 1) $('matches').textContent = 'No matches'; });
  $('previous').onclick = () => { if (viewer.currentPageNumber > 1) viewer.currentPageNumber--; };
  $('next').onclick = () => { if (viewer.currentPageNumber < viewer.pagesCount) viewer.currentPageNumber++; };
  $('page').onchange = () => { viewer.currentPageNumber = Math.max(1, Math.min(viewer.pagesCount, parseInt($('page').value, 10) || 1)); $('page').value = viewer.currentPageNumber; };
  $('zoom').onchange = () => { viewer.currentScaleValue = $('zoom').value; };
  $('zoomIn').onclick = () => { viewer.currentScale = Math.min(4, viewer.currentScale * 1.2); };
  $('zoomOut').onclick = () => { viewer.currentScale = Math.max(.25, viewer.currentScale / 1.2); };
  function find(previous = false, again = false) {
    eventBus.dispatch('find', { source: window, type: again ? 'again' : '', query: $('search').value,
      phraseSearch: true, caseSensitive: false, entireWord: false, highlightAll: true, findPrevious: previous, matchDiacritics: false });
  }
  $('search').oninput = () => find();
  $('search').onkeydown = e => { if (e.key === 'Enter') { e.preventDefault(); find(e.shiftKey, true); } };
  $('findPrevious').onclick = () => find(true, true);
  $('findNext').onclick = () => find(false, true);
  document.addEventListener('keydown', e => {
    if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'f') { e.preventDefault(); $('search').focus(); $('search').select(); }
  });
  const task = pdfjs.getDocument({ url: './document.pdf', cMapUrl: './cmaps/', cMapPacked: true,
    standardFontDataUrl: './standard_fonts/', wasmUrl: './wasm/', isEvalSupported: false, enableXfa: false });
  task.onPassword = (updatePassword, reason) => {
    $('passwordLabel').textContent = reason === pdfjs.PasswordResponses.INCORRECT_PASSWORD ? 'Incorrect password. Try again.' : 'This PDF needs a password.';
    $('password').value = '';
    if (!$('passwordDialog').open) $('passwordDialog').showModal();
    $('password').focus();
    $('passwordForm').onsubmit = e => { e.preventDefault(); const password = $('password').value; $('password').value = ''; $('passwordDialog').close(); updatePassword(password); };
  };
  $('cancelPassword').onclick = () => { $('passwordDialog').close(); task.destroy(); status.textContent = 'Opening this PDF was cancelled.'; layout(); };
  $('passwordDialog').addEventListener('cancel', () => { task.destroy(); status.textContent = 'Opening this PDF was cancelled.'; layout(); });
  window.addEventListener('pagehide', () => { task.destroy(); }, { once: true });
  const pdf = await task.promise;
  viewer.setDocument(pdf); linkService.setDocument(pdf, null); findController.setDocument(pdf);
} catch { fail(); }
