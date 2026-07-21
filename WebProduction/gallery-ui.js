(function(){
  'use strict';

  const SESSION_KEY='dmp_prod_session';
  const DEVICE_KEY='dmp_prod_device_id';
  const TYPE_LABELS={Avatar:'الصور الشخصية',ProfileBackground:'الخلفيات',Frame:'الإطارات',Effect:'المؤثرات',Title:'الألقاب',Badge:'الشارات',Emblem:'الشعارات',EmblemBackground:'خلفيات الشعارات',TeamColor:'ألوان الفريق',TeamBanner:'رايات الفريق',TeamEffect:'مؤثرات الفريق',TeamLivingEmblem:'الشعارات الحية'};
  let items=[];
  let category='All';
  let query='';
  let loading=false;
  let assetAliases={};

  function session(){try{return JSON.parse(localStorage.getItem(SESSION_KEY)||'null')}catch{return null}}
  function deviceId(){return localStorage.getItem(DEVICE_KEY)||''}
  function headers(){const token=session()?.accessToken;return token?{'Authorization':`Bearer ${token}`,'X-Device-Id':deviceId()}:{} }
  function escapeHtml(value){return String(value??'').replace(/[&<>'"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]))}
  function unwrap(row){return row?.payload??row}
  async function api(path){const response=await fetch(`${location.origin}${path}`,{headers:{...headers()}});const payload=await response.json().catch(()=>null);if(!response.ok)throw new Error(payload?.message||'تعذر تحميل المعرض.');return payload}

  function normalize(row){
    const v=unwrap(row)||{};
    const assetId=String(v.assetId||v.itemId||v.id||'').trim();
    const type=String(v.storeTypeId||v.itemType||v.assetType||v.type||'').trim();
    return {...v,assetId,itemType:type,title:String(v.title||v.name||assetId),description:String(v.description||''),collection:String(v.collectionId||v.collection||'Default'),rarity:String(v.rarity||'Common'),tag:String(v.tag||v.badge||''),isPublished:v.isPublished!==false&&String(v.publishState||'Published').toLowerCase()!=='draft'&&String(v.publishState||'Published').toLowerCase()!=='hidden',previewAsset:String(v.previewAsset||v.imagePath||v.assetPath||v.thumbnailPath||'')};
  }

  async function loadManifest(){
    try{
      const response=await fetch('/assets/asset-manifest.json',{cache:'no-cache'});
      const manifest=response.ok?await response.json():null;
      assetAliases=manifest?.aliases||{};
    }catch{assetAliases={}}
  }

  function assetUrl(pathValue){
    const value=String(pathValue||'').trim();
    if(!value)return '';
    if(/^https?:\/\//i.test(value)||value.startsWith('data:')||value.startsWith('blob:'))return value;
    const clean=value.replaceAll('\\','/').replace(/^\.\//,'').replace(/^\/+/, '');
    const basename=clean.split('/').pop();
    const candidates=[clean,clean.toLowerCase(),basename,basename?.toLowerCase()].filter(Boolean);
    for(const key of candidates)if(assetAliases[key])return assetAliases[key];
    if(clean.startsWith('WebProduction/'))return `/${clean}`;
    if(clean.startsWith('assets/'))return `/${clean}`;
    return `/assets/maui-images/${basename}`;
  }

  async function load(){
    const [rows]=await Promise.all([api('/api/v1/store-items?includeDeleted=false').catch(()=>[]),loadManifest()]);
    items=(rows||[]).map(normalize).filter(x=>x.assetId&&x.isPublished);
  }

  function categories(){return ['All',...new Set(items.map(x=>x.itemType).filter(Boolean))]}
  function filtered(){const q=query.toLowerCase();return items.filter(x=>(category==='All'||x.itemType===category)&&(!q||`${x.title} ${x.description} ${x.collection} ${x.rarity}`.toLowerCase().includes(q)))}
  function preview(item){const src=assetUrl(item.previewAsset);const fallback=escapeHtml((item.title||'✦').charAt(0)||'✦');return `<div class="gallery-preview">${src?`<img src="${escapeHtml(src)}" alt="${escapeHtml(item.title)}" loading="lazy" onerror="this.remove();this.parentElement.dataset.fallback='${fallback}'">`:`<span>${fallback}</span>`}</div>`}
  function card(item){return `<article class="gallery-card" data-gallery-id="${escapeHtml(item.assetId)}">${preview(item)}<div class="gallery-card-body"><div class="gallery-meta"><span>${escapeHtml(TYPE_LABELS[item.itemType]||item.itemType||'عنصر')}</span><span class="rarity rarity-${escapeHtml(item.rarity.toLowerCase())}">${escapeHtml(item.rarity)}</span></div><h3>${escapeHtml(item.title)}</h3><p>${escapeHtml(item.description||'عنصر هوية بصرية رسمي')}</p><small>${escapeHtml(item.collection||'Default')}${item.tag?` · ${escapeHtml(item.tag)}`:''}</small></div><button class="secondary gallery-store-link" data-route="store">عرض في المتجر</button></article>`}

  function render(){
    const main=document.querySelector('main');if(!main)return;
    const rows=filtered();
    main.innerHTML=`<h1 class="page-title">المعرض</h1><p class="page-subtitle">الكتالوج المرئي الرسمي للعناصر المنشورة.</p><section class="card gallery-toolbar"><input id="gallerySearch" class="field" placeholder="بحث في المعرض" value="${escapeHtml(query)}"><div class="gallery-categories">${categories().map(c=>`<button class="${category===c?'primary':'secondary'}" data-gallery-category="${escapeHtml(c)}">${c==='All'?'الكل':escapeHtml(TYPE_LABELS[c]||c)}</button>`).join('')}</div></section><section class="gallery-grid">${rows.length?rows.map(card).join(''):'<div class="card"><p class="muted">لا توجد عناصر منشورة مطابقة.</p></div>'}</section>`;
    bind();
  }

  function bind(){
    const search=document.getElementById('gallerySearch');if(search)search.oninput=()=>{query=search.value;render()};
    document.querySelectorAll('[data-gallery-category]').forEach(b=>b.onclick=()=>{category=b.dataset.galleryCategory;render()});
  }

  async function open(){
    if(loading)return;loading=true;
    const main=document.querySelector('main');if(main)main.innerHTML='<section class="card"><p class="muted">جارٍ تحميل المعرض...</p></section>';
    try{await load();render()}catch(error){if(main)main.innerHTML=`<h1 class="page-title">المعرض</h1><section class="card"><p class="message">${escapeHtml(error.message)}</p></section>`}finally{loading=false}
  }

  document.addEventListener('click',event=>{const route=event.target.closest('[data-route]')?.dataset.route;if(route==='gallery')setTimeout(open,0)},true);
  const observer=new MutationObserver(()=>{const title=document.querySelector('main .page-title')?.textContent?.trim();if(title==='المعرض'&&!document.querySelector('.gallery-toolbar'))open()});
  observer.observe(document.documentElement,{subtree:true,childList:true});
  window.DominoGalleryUI={open,assetUrl,loadManifest};
})();
