(function(){
  'use strict';
  function ensureButton(){
    const nav=document.querySelector('.bottom-nav-inner');
    if(!nav||nav.querySelector('[data-gallery-launch]'))return;
    const button=document.createElement('button');
    button.className='nav-button gallery-nav-injected';
    button.dataset.galleryLaunch='true';
    button.innerHTML='<span>▦</span>المعرض';
    button.onclick=event=>{event.preventDefault();event.stopPropagation();window.DominoGalleryUI?.open();};
    nav.appendChild(button);
  }
  document.addEventListener('click',event=>{
    const link=event.target.closest('.gallery-store-link');
    if(link){setTimeout(()=>window.DominoStoreUI?.open('store'),0)}
  },true);
  const observer=new MutationObserver(ensureButton);
  observer.observe(document.documentElement,{subtree:true,childList:true});
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',ensureButton);else ensureButton();
})();