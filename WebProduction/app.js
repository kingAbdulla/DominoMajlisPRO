const API_BASE = location.origin;
const app = document.getElementById('app');

const storage = {
  get session(){ try{return JSON.parse(localStorage.getItem('dmp_prod_session')||'null')}catch{return null}},
  set session(value){ value?localStorage.setItem('dmp_prod_session',JSON.stringify(value)):localStorage.removeItem('dmp_prod_session') },
  get deviceId(){ let value=localStorage.getItem('dmp_prod_device_id'); if(!value){value=`WEB-${crypto.randomUUID()}`;localStorage.setItem('dmp_prod_device_id',value)} return value }
};

const state={session:storage.session,route:'home',teams:[],online:false};
const headers=()=>state.session?.accessToken?{'Authorization':`Bearer ${state.session.accessToken}`,'X-Device-Id':storage.deviceId}:{};

async function api(path,options={}){
  const response=await fetch(`${API_BASE}${path}`,{...options,headers:{'Content-Type':'application/json',...headers(),...(options.headers||{})}});
  if(response.status===401){storage.session=null;state.session=null;renderAuth('انتهت الجلسة. سجّل الدخول مجددًا.');throw new Error('unauthorized')}
  const payload=response.status===204?null:await response.json().catch(()=>null);
  if(!response.ok) throw new Error(payload?.message||'تعذر تنفيذ العملية.');
  state.online=true;return payload;
}

function escapeHtml(value){return String(value??'').replace(/[&<>'"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]))}

function renderAuth(error=''){
  app.innerHTML=`<section class="boot-screen" style="display:block;min-height:auto;padding-top:6vh">
    <div class="brand-mark">♛</div><h1>Domino Majlis PRO</h1><p>الحساب السحابي الرسمي</p>
    <div class="card" style="max-width:520px;margin:28px auto;text-align:right">
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:12px">
        <button class="primary" id="loginMode" style="margin:0">تسجيل الدخول</button>
        <button class="secondary" id="registerMode">إنشاء حساب</button>
      </div>
      <label>اسم المستخدم</label><input class="field" id="username" autocomplete="username">
      <label style="display:block;margin-top:12px">كلمة المرور</label><input class="field" id="password" type="password" autocomplete="current-password">
      <button class="primary" id="submitAuth">متابعة</button><p class="message" id="authMessage">${escapeHtml(error)}</p>
    </div></section>`;
  let mode='login';
  const login=document.getElementById('loginMode'),register=document.getElementById('registerMode');
  login.onclick=()=>{mode='login';login.className='primary';login.style.margin='0';register.className='secondary'};
  register.onclick=()=>{mode='register';register.className='primary';register.style.margin='0';login.className='secondary'};
  document.getElementById('submitAuth').onclick=async()=>{
    const username=document.getElementById('username').value.trim(),password=document.getElementById('password').value,msg=document.getElementById('authMessage');
    if(username.length<3||password.length<8){msg.textContent='اسم المستخدم 3 أحرف على الأقل وكلمة المرور 8 أحرف على الأقل.';return}
    msg.textContent='جارٍ الاتصال...';
    try{const session=await api(`/api/preview/${mode}`,{method:'POST',body:JSON.stringify({username,password,deviceId:storage.deviceId})});storage.session=session;state.session=session;await bootAuthenticated()}
    catch(error){if(error.message!=='unauthorized')msg.textContent=error.message}
  };
}

async function bootAuthenticated(){
  try{await Promise.all([loadTeams(),health()]);renderShell()}catch{renderShell()}
}
async function health(){try{const r=await fetch(`${API_BASE}/api/health`);state.online=r.ok}catch{state.online=false}}
async function loadTeams(){state.teams=await api('/api/preview/me/teams').catch(()=>[])}

function topBar(){
  const user=state.session?.user||{};const initial=(user.displayName||'?').charAt(0).toUpperCase();
  return `<header class="topbar">
    <div class="wallet-chip"><span>🪙</span><strong>0</strong><button aria-label="شراء عملات">+</button></div>
    <div class="wallet-chip"><span>💎</span><strong>0</strong><button aria-label="شراء جواهر">+</button></div>
    <button class="avatar-button" data-route="profile"><span style="font-size:26px;color:var(--gold2)">${escapeHtml(initial)}</span><span class="level-badge">1</span></button>
    <button class="icon-button" data-route="settings">⚙</button>
  </header>`;
}

const routes={
  home:()=>`<h1 class="page-title">الرئيسية</h1><p class="page-subtitle">مجلسك، فرقك، وبياناتك السحابية في مكان واحد.</p>
    <section class="card hero"><h2>مرحبًا ${escapeHtml(state.session?.user?.displayName||'')}</h2><p>الحساب متصل بقاعدة Domino Majlis PRO السحابية.</p>
      <div class="metrics"><div class="metric"><b>${state.teams.length}</b><span>الفرق</span></div><div class="metric"><b>0</b><span>المباريات</span></div><div class="metric"><b>${state.online?'✓':'!'}</b><span>المزامنة</span></div></div></section>
    <h2 style="color:var(--gold2)">وصول سريع</h2><div class="section-grid">
      <button class="action-card" data-route="teams"><h3>الفرق</h3><p>إنشاء الفرق وإدارتها وحفظها في الحساب.</p></button>
      <button class="action-card" data-route="profile"><h3>الملف الشخصي</h3><p>بيانات الحساب والهوية ومستوى اللاعب.</p></button>
    </div>`,
  teams:()=>`<h1 class="page-title">الفرق</h1><p class="page-subtitle">إدارة فرق الحساب السحابي.</p>
    <section class="card"><label>اسم الفريق الجديد</label><input id="teamName" class="field" placeholder="اكتب اسم الفريق"><button id="createTeam" class="primary">إنشاء الفريق</button><p id="teamMessage" class="message"></p></section>
    <section class="card"><h2 style="margin-top:0;color:var(--gold2)">فرقك</h2><div id="teamList">${teamList()}</div></section>`,
  profile:()=>{const u=state.session?.user||{};return `<h1 class="page-title">الملف الشخصي</h1><p class="page-subtitle">هوية الحساب السحابي.</p>
    <section class="card"><div style="display:flex;gap:14px;align-items:center"><div class="avatar-button" style="display:grid;place-items:center;font-size:28px;color:var(--gold2)">${escapeHtml((u.displayName||'?')[0])}</div><div><h2 style="margin:0">${escapeHtml(u.displayName||'')}</h2><p style="color:var(--muted)">عضو Domino Majlis PRO</p></div></div>
    <div class="metrics"><div class="metric"><b>0</b><span>XP</span></div><div class="metric"><b>100%</b><span>الثقة</span></div><div class="metric"><b>—</b><span>الرتبة</span></div></div></section>`,
  settings:()=>`<h1 class="page-title">الإعدادات</h1><p class="page-subtitle">إدارة الجلسة والاتصال.</p>
    <section class="card"><div class="list-item"><div class="emblem">☁</div><div><b>حالة السحابة</b><br><small>${state.online?'متصل':'غير متصل'}</small></div><span>${state.online?'✓':'!'}</span></div>
    <button id="logout" class="secondary" style="margin-top:16px">تسجيل الخروج</button></section>`
};

function teamList(){return state.teams.length?state.teams.map(team=>`<article class="list-item"><div class="emblem">♜</div><div><b>${escapeHtml(team.name)}</b><br><small>${escapeHtml(team.teamId||'')}</small></div><span>‹</span></article>`).join(''):'<p style="color:var(--muted)">لا توجد فرق بعد.</p>'}

function nav(){return `<nav class="bottom-nav"><div class="bottom-nav-inner">
  <button class="nav-button ${state.route==='home'?'active':''}" data-route="home"><span>⌂</span>الرئيسية</button>
  <button class="nav-button ${state.route==='teams'?'active':''}" data-route="teams"><span>♜</span>الفرق</button>
  <button class="nav-button ${state.route==='profile'?'active':''}" data-route="profile"><span>●</span>حسابي</button>
  <button class="nav-button ${state.route==='settings'?'active':''}" data-route="settings"><span>⚙</span>الإعدادات</button>
</div></nav>`}

function renderShell(){app.innerHTML=`${topBar()}<main>${routes[state.route]()}</main>${nav()}`;bindNavigation();bindRouteActions()}
function bindNavigation(){document.querySelectorAll('[data-route]').forEach(el=>el.onclick=()=>{state.route=el.dataset.route;renderShell()})}
function bindRouteActions(){
  if(state.route==='teams')document.getElementById('createTeam').onclick=async()=>{const input=document.getElementById('teamName'),msg=document.getElementById('teamMessage'),name=input.value.trim();if(!name){msg.textContent='اسم الفريق مطلوب.';return}msg.textContent='جارٍ الحفظ...';try{await api('/api/preview/me/teams',{method:'POST',body:JSON.stringify({name})});await loadTeams();renderShell()}catch(error){msg.textContent=error.message}};
  if(state.route==='settings')document.getElementById('logout').onclick=async()=>{try{await api('/api/preview/logout',{method:'POST'})}catch{}storage.session=null;state.session=null;renderAuth()};
}

state.session?bootAuthenticated():renderAuth();
