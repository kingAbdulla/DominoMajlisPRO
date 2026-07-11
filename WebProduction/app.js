const API_BASE = location.origin;
const app = document.getElementById('app');

const storage = {
  get session(){ try{return JSON.parse(localStorage.getItem('dmp_prod_session')||'null')}catch{return null}},
  set session(value){ value?localStorage.setItem('dmp_prod_session',JSON.stringify(value)):localStorage.removeItem('dmp_prod_session') },
  get deviceId(){ let value=localStorage.getItem('dmp_prod_device_id'); if(!value){value=`WEB-${crypto.randomUUID()}`;localStorage.setItem('dmp_prod_device_id',value)} return value },
  get activeMatch(){ try{return JSON.parse(localStorage.getItem('dmp_prod_active_match')||'null')}catch{return null}},
  set activeMatch(value){ value?localStorage.setItem('dmp_prod_active_match',JSON.stringify(value)):localStorage.removeItem('dmp_prod_active_match') }
};

const state={session:storage.session,route:'home',teams:[],matches:[],online:false,activeMatch:storage.activeMatch};
const headers=()=>state.session?.accessToken?{'Authorization':`Bearer ${state.session.accessToken}`,'X-Device-Id':storage.deviceId}:{};

async function api(path,options={}){
  const response=await fetch(`${API_BASE}${path}`,{...options,headers:{'Content-Type':'application/json',...headers(),...(options.headers||{})}});
  if(response.status===401){storage.session=null;state.session=null;renderAuth('انتهت الجلسة. سجّل الدخول مجددًا.');throw new Error('unauthorized')}
  const payload=response.status===204?null:await response.json().catch(()=>null);
  if(!response.ok) throw new Error(payload?.message||'تعذر تنفيذ العملية.');
  state.online=true;return payload;
}

function escapeHtml(value){return String(value??'').replace(/[&<>'"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]))}
function guid(){return crypto.randomUUID()}
function nowIso(){return new Date().toISOString()}
function cloudPayload(record){return record?.payload??record}

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
  await Promise.allSettled([loadTeams(),loadMatches(),health()]);
  renderShell();
}
async function health(){try{const r=await fetch(`${API_BASE}/api/health`);state.online=r.ok}catch{state.online=false}}
async function loadTeams(){state.teams=await api('/api/preview/me/teams').catch(()=>[])}
async function loadMatches(){const rows=await api('/api/v1/matches?includeDeleted=false').catch(()=>[]);state.matches=(rows||[]).map(cloudPayload).filter(Boolean).sort((a,b)=>new Date(b.lastPlayedTime||b.matchDate)-new Date(a.lastPlayedTime||a.matchDate))}

function topBar(){
  const user=state.session?.user||{};const initial=(user.displayName||'?').charAt(0).toUpperCase();
  return `<header class="topbar">
    <div class="wallet-chip"><span>🪙</span><strong>0</strong><button aria-label="شراء عملات">+</button></div>
    <div class="wallet-chip"><span>💎</span><strong>0</strong><button aria-label="شراء جواهر">+</button></div>
    <button class="avatar-button" data-route="profile"><span style="font-size:26px;color:var(--gold2)">${escapeHtml(initial)}</span><span class="level-badge">1</span></button>
    <button class="icon-button" data-route="settings">⚙</button>
  </header>`;
}

function teamOptions(selected=''){return `<option value="">اختر فريقًا</option>${state.teams.map(t=>`<option value="${escapeHtml(t.teamId)}" ${t.teamId===selected?'selected':''}>${escapeHtml(t.name)}</option>`).join('')}`}
function teamName(id){return state.teams.find(t=>t.teamId===id)?.name||id||'فريق'}

const routes={
  home:()=>`<h1 class="page-title">الرئيسية</h1><p class="page-subtitle">مجلسك، فرقك، ومبارياتك السحابية في مكان واحد.</p>
    <section class="card hero"><h2>مرحبًا ${escapeHtml(state.session?.user?.displayName||'')}</h2><p>الحساب متصل بقاعدة Domino Majlis PRO السحابية.</p>
      <div class="metrics"><div class="metric"><b>${state.teams.length}</b><span>الفرق</span></div><div class="metric"><b>${state.matches.length}</b><span>المباريات</span></div><div class="metric"><b>${state.online?'✓':'!'}</b><span>المزامنة</span></div></div></section>
    <h2 style="color:var(--gold2)">وصول سريع</h2><div class="section-grid">
      <button class="action-card" data-route="teams"><h3>الفرق</h3><p>إنشاء الفرق وإدارتها وحفظها في الحساب.</p></button>
      <button class="action-card" data-route="matches"><h3>المباريات</h3><p>بدء مباراة، تسجيل الجولات، وحفظ النتيجة.</p></button>
      <button class="action-card" data-route="history"><h3>السجل</h3><p>مراجعة المباريات السابقة ونتائجها.</p></button>
      <button class="action-card" data-route="profile"><h3>الملف الشخصي</h3><p>بيانات الحساب والهوية ومستوى اللاعب.</p></button>
    </div>`,
  teams:()=>`<h1 class="page-title">الفرق</h1><p class="page-subtitle">إدارة فرق الحساب السحابي.</p>
    <section class="card"><label>اسم الفريق الجديد</label><input id="teamName" class="field" placeholder="اكتب اسم الفريق"><button id="createTeam" class="primary">إنشاء الفريق</button><p id="teamMessage" class="message"></p></section>
    <section class="card"><h2 style="margin-top:0;color:var(--gold2)">فرقك</h2><div id="teamList">${teamList()}</div></section>`,
  matches:()=>state.activeMatch?matchBoard():matchSetup(),
  history:()=>historyView(),
  profile:()=>{const u=state.session?.user||{};return `<h1 class="page-title">الملف الشخصي</h1><p class="page-subtitle">هوية الحساب السحابي.</p>
    <section class="card"><div style="display:flex;gap:14px;align-items:center"><div class="avatar-button" style="display:grid;place-items:center;font-size:28px;color:var(--gold2)">${escapeHtml((u.displayName||'?')[0])}</div><div><h2 style="margin:0">${escapeHtml(u.displayName||'')}</h2><p style="color:var(--muted)">عضو Domino Majlis PRO</p></div></div>
    <div class="metrics"><div class="metric"><b>0</b><span>XP</span></div><div class="metric"><b>100%</b><span>الثقة</span></div><div class="metric"><b>${state.matches.filter(m=>m.winnerTeamId).length}</b><span>المباريات</span></div></div></section>`,
  settings:()=>`<h1 class="page-title">الإعدادات</h1><p class="page-subtitle">إدارة الجلسة والاتصال.</p>
    <section class="card"><div class="list-item"><div class="emblem">☁</div><div><b>حالة السحابة</b><br><small>${state.online?'متصل':'غير متصل'}</small></div><span>${state.online?'✓':'!'}</span></div>
    <button id="logout" class="secondary" style="margin-top:16px">تسجيل الخروج</button></section>`
};

function matchSetup(){
  const enough=state.teams.length>=2;
  return `<h1 class="page-title">مباراة جديدة</h1><p class="page-subtitle">اختيار الفريقين ونظام القواعد.</p>
    <section class="card">
      <label>الفريق الأول</label><select id="team1" class="field">${teamOptions()}</select>
      <label style="display:block;margin-top:12px">الفريق الثاني</label><select id="team2" class="field">${teamOptions()}</select>
      <label style="display:block;margin-top:12px">نظام المباراة</label><select id="rules" class="field"><option value="local">محلي</option><option value="international">دولي</option></select>
      <label style="display:block;margin-top:12px"><input type="checkbox" id="ranked"> مباراة مصنفة</label>
      <button id="startMatch" class="primary" ${enough?'':'disabled'}>بدء المباراة</button>
      <p id="matchMessage" class="message">${enough?'':'يجب إنشاء فريقين على الأقل أولًا.'}</p>
    </section>`;
}

function matchBoard(){
  const m=state.activeMatch;
  return `<h1 class="page-title">المباراة الجارية</h1><p class="page-subtitle">الجولة ${m.roundNumber||1} — ${m.isLocalRules?'قواعد محلية':'قواعد دولية'}</p>
    <section class="card score-board">
      <div class="score-team"><span>${escapeHtml(m.team1Name)}</span><b>${m.team1Score}</b></div>
      <div class="versus">VS</div>
      <div class="score-team"><span>${escapeHtml(m.team2Name)}</span><b>${m.team2Score}</b></div>
    </section>
    <section class="card">
      <h2 style="margin-top:0;color:var(--gold2)">إضافة جولة</h2>
      <div class="round-grid"><div><label>${escapeHtml(m.team1Name)}</label><input id="round1" class="field" inputmode="numeric" type="number" min="0" value="0"></div><div><label>${escapeHtml(m.team2Name)}</label><input id="round2" class="field" inputmode="numeric" type="number" min="0" value="0"></div></div>
      <button id="addRound" class="primary">حفظ الجولة</button><p id="roundMessage" class="message"></p>
    </section>
    <section class="card"><h2 style="margin-top:0;color:var(--gold2)">سجل الجولات</h2>${roundHistory(m)}</section>
    <div class="section-grid"><button id="saveExit" class="secondary">حفظ والخروج</button><button id="cancelMatch" class="secondary">إلغاء المباراة</button></div>`;
}

function roundHistory(m){return m.roundsHistory?.length?m.roundsHistory.map((r,i)=>`<div class="list-item"><div class="emblem">${i+1}</div><div><b>${r.team1RoundScore} - ${r.team2RoundScore}</b><br><small>${escapeHtml(m.team1Name)} / ${escapeHtml(m.team2Name)}</small></div><span>${new Date(r.playedAt).toLocaleTimeString('ar-IQ',{hour:'2-digit',minute:'2-digit'})}</span></div>`).join(''):'<p style="color:var(--muted)">لم تُسجل جولات بعد.</p>'}

function historyView(){return `<h1 class="page-title">سجل المباريات</h1><p class="page-subtitle">المباريات المحفوظة في حسابك.</p><section class="card">${state.matches.length?state.matches.map(m=>`<article class="match-history"><div><b>${escapeHtml(m.team1Name)} <span>${m.team1Score}</span></b><small>ضد</small><b>${escapeHtml(m.team2Name)} <span>${m.team2Score}</span></b></div><div class="history-meta"><strong>${m.isFinished?'منتهية':'غير مكتملة'}</strong><small>${new Date(m.matchDate||m.lastPlayedTime).toLocaleDateString('ar-IQ')}</small></div></article>`).join(''):'<p style="color:var(--muted)">لا توجد مباريات محفوظة.</p>'}</section>`}

function evaluateWinner(m){
  const max=Math.max(m.team1Score,m.team2Score),min=Math.min(m.team1Score,m.team2Score);
  const winnerId=m.team1Score>m.team2Score?m.team1Id:m.team2Id;
  const winnerName=m.team1Score>m.team2Score?m.team1Name:m.team2Name;
  if(m.team1Score===m.team2Score)return null;
  if(m.isLocalRules&&max>=101&&min<25)return {winnerId,winnerName,hasMeles:true};
  if(max>=151)return {winnerId,winnerName,hasMeles:false};
  return null;
}

async function persistMatch(match){await api(`/api/v1/matches/${encodeURIComponent(match.matchId)}`,{method:'PUT',body:JSON.stringify({payload:match})})}
async function completeRound(){
  const m=state.activeMatch;const msg=document.getElementById('roundMessage');
  const s1=Math.max(0,Number(document.getElementById('round1').value||0));const s2=Math.max(0,Number(document.getElementById('round2').value||0));
  if(s1===0&&s2===0){msg.textContent='يجب إدخال نتيجة لجولة واحدة على الأقل.';return}
  m.team1Score+=s1;m.team2Score+=s2;m.roundsHistory.push({roundNumber:m.roundNumber,team1RoundScore:s1,team2RoundScore:s2,playedAt:nowIso()});m.roundNumber+=1;m.lastPlayedTime=nowIso();
  const winner=evaluateWinner(m);if(winner){m.isFinished=true;m.matchEndDate=nowIso();m.winnerTeamId=winner.winnerId;m.winnerTeamName=winner.winnerName;m.winnerTeam=winner.winnerName;m.hasMeles=winner.hasMeles;m.isLocked=true}
  storage.activeMatch=m;await persistMatch(m);await loadMatches();
  if(winner){storage.activeMatch=null;state.activeMatch=null;state.route='history';renderShell();return}
  renderShell();
}

function teamList(){return state.teams.length?state.teams.map(team=>`<article class="list-item"><div class="emblem">♜</div><div><b>${escapeHtml(team.name)}</b><br><small>${escapeHtml(team.teamId||'')}</small></div><span>‹</span></article>`).join(''):'<p style="color:var(--muted)">لا توجد فرق بعد.</p>'}
function nav(){return `<nav class="bottom-nav"><div class="bottom-nav-inner">
  <button class="nav-button ${state.route==='home'?'active':''}" data-route="home"><span>⌂</span>الرئيسية</button>
  <button class="nav-button ${state.route==='teams'?'active':''}" data-route="teams"><span>♜</span>الفرق</button>
  <button class="nav-button ${state.route==='matches'?'active':''}" data-route="matches"><span>◆</span>مباراة</button>
  <button class="nav-button ${state.route==='history'?'active':''}" data-route="history"><span>≡</span>السجل</button>
  <button class="nav-button ${state.route==='profile'?'active':''}" data-route="profile"><span>●</span>حسابي</button>
</div></nav>`}

function renderShell(){app.innerHTML=`${topBar()}<main>${routes[state.route]()}</main>${nav()}`;bindNavigation();bindRouteActions()}
function bindNavigation(){document.querySelectorAll('[data-route]').forEach(el=>el.onclick=()=>{state.route=el.dataset.route;renderShell()})}
function bindRouteActions(){
  if(state.route==='teams')document.getElementById('createTeam').onclick=async()=>{const input=document.getElementById('teamName'),msg=document.getElementById('teamMessage'),name=input.value.trim();if(!name){msg.textContent='اسم الفريق مطلوب.';return}msg.textContent='جارٍ الحفظ...';try{await api('/api/preview/me/teams',{method:'POST',body:JSON.stringify({name})});await loadTeams();renderShell()}catch(error){msg.textContent=error.message}};
  if(state.route==='matches'&&!state.activeMatch){const btn=document.getElementById('startMatch');if(btn)btn.onclick=()=>{const t1=document.getElementById('team1').value,t2=document.getElementById('team2').value,msg=document.getElementById('matchMessage');if(!t1||!t2||t1===t2){msg.textContent='اختر فريقين مختلفين.';return}const m={matchId:guid(),team1Id:t1,team2Id:t2,team1Name:teamName(t1),team2Name:teamName(t2),team1Players:'',team2Players:'',team1Score:0,team2Score:0,team1Player1Id:'',team1Player2Id:'',team2Player1Id:'',team2Player2Id:'',roundNumber:1,isLocalRules:document.getElementById('rules').value==='local',matchDate:nowIso(),matchEndDate:null,matchDurationMinutes:0,roundsHistory:[],winnerTeam:'',winnerTeamName:'',winnerTeamId:'',hasMeles:false,isDraw:false,isFinished:false,isLocked:false,lastPlayedTime:nowIso(),displayTitle:`${teamName(t1)} ضد ${teamName(t2)}`,rankedMatch:document.getElementById('ranked').checked,matchVerificationCode:'',isVerified:false,team1Emblem:'',team2Emblem:'',team1ColorHex:'#FFD700',team2ColorHex:'#FFD700'};state.activeMatch=m;storage.activeMatch=m;renderShell()}};
  if(state.route==='matches'&&state.activeMatch){document.getElementById('addRound').onclick=()=>completeRound().catch(e=>document.getElementById('roundMessage').textContent=e.message);document.getElementById('saveExit').onclick=async()=>{await persistMatch(state.activeMatch);await loadMatches();state.route='home';renderShell()};document.getElementById('cancelMatch').onclick=()=>{if(confirm('هل تريد إلغاء المباراة الجارية؟')){storage.activeMatch=null;state.activeMatch=null;renderShell()}}}
  if(state.route==='settings')document.getElementById('logout').onclick=async()=>{try{await api('/api/preview/logout',{method:'POST'})}catch{}storage.session=null;state.session=null;renderAuth()};
}

state.session?bootAuthenticated():renderAuth();
