"use strict";
/* Agenda incorporada: funciona mesmo se o antigo js/horarios.js estiver em cache ou ausente. */
// PetVille: hora local do estabelecimento (America/Sao_Paulo).
// A validação definitiva também é aplicada na API ASP.NET Core.
(function(global){
  const ABRE=8*60, FECHA=18*60, PASSO=30, DIAS_ANTECEDENCIA=90;
  const pad=n=>String(n).padStart(2,"0");
  const chaveData=d=>`${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}`;
  function parseData(s){
    if(!/^\d{4}-\d{2}-\d{2}$/.test(s||""))return null;
    const [y,m,d]=s.split("-").map(Number), dt=new Date(y,m-1,d);
    return dt.getFullYear()===y&&dt.getMonth()===m-1&&dt.getDate()===d?dt:null;
  }
  function diaUtil(data){const d=data.getDay();return d>=1&&d<=5;}
  function datasDisponiveis(agora=new Date()){
    const inicio=new Date(agora.getFullYear(),agora.getMonth(),agora.getDate());
    const fmt=new Intl.DateTimeFormat("pt-BR",{weekday:"long",day:"2-digit",month:"long"});
    const datas=[];
    for(let i=0;i<=DIAS_ANTECEDENCIA;i++){
      const d=new Date(inicio);d.setDate(inicio.getDate()+i);
      if(diaUtil(d))datas.push({valor:chaveData(d),legenda:fmt.format(d)});
    }
    return datas;
  }
  function horariosDisponiveis(data, duracao=60, agora=new Date()){
    const dt=parseData(data);if(!dt||!diaUtil(dt))return [];
    const hoje=new Date(agora.getFullYear(),agora.getMonth(),agora.getDate());
    const fim=new Date(hoje);fim.setDate(fim.getDate()+DIAS_ANTECEDENCIA);
    if(dt<hoje||dt>fim)return [];
    const minutos=Math.ceil(Math.max(1,Number(duracao)||60));
    const out=[];
    for(let t=ABRE;t+minutos<=FECHA;t+=PASSO){
      const h=`${pad(Math.floor(t/60))}:${pad(t%60)}`;
      if(new Date(`${data}T${h}:00`)>agora)out.push(h);
    }
    return out;
  }
  function validar(data,horario,duracao,agora=new Date()){
    const d=parseData(data);
    if(!d||!diaUtil(d))return "Atendemos somente de segunda a sexta-feira.";
    if(!/^\d{2}:\d{2}$/.test(horario||""))return "Escolha um horário disponível.";
    if(!horariosDisponiveis(data,duracao,agora).includes(horario))
      return "Esse horário não está disponível. Atendemos das 8h às 18h e o serviço precisa terminar até o fechamento.";
    return "";
  }
  global.PetVilleAgenda=Object.freeze({datasDisponiveis,horariosDisponiveis,validar,ABRE,FECHA,PASSO});
})(typeof window!=="undefined"?window:globalThis);

/* Interações do cliente */
const el=id=>document.getElementById(id);
const esc=s=>String(s??"").replace(/[&<>"']/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
let token=sessionStorage.getItem("petville_demo_token")||"", registro=false;
let catalogoServicos=[];
const paw=`<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="8" cy="7" r="1.5"/><circle cx="14" cy="6" r="1.5"/><circle cx="19" cy="9" r="1.5"/><circle cx="4.5" cy="12" r="1.5"/><path d="M12 11c-2 0-3.3 2-5.6 4.3-2 2-1.2 5 1.4 5 .9 0 2.2-.7 4.2-.7s3.3.7 4.2.7c2.6 0 3.4-3 1.4-5C15.3 13 14 11 12 11Z"/></svg>`;
const friendlyDate=s=>{const d=new Date(s);return Number.isNaN(d.valueOf())?"—":d.toLocaleString("pt-BR",{dateStyle:"short",timeStyle:"short"});};
const money=v=>Number(v||0).toLocaleString("pt-BR",{style:"currency",currency:"BRL"});
async function request(url,method="GET",data){const r=await fetch(url,{method,headers:{...(data?{"Content-Type":"application/json"}:{}),...(token?{Authorization:"Bearer "+token}:{})},...(data?{body:JSON.stringify(data)}:{})});if(!r.ok){let t=await r.text();try{let j=JSON.parse(t);t=j.mensagem||j.title||t}catch{}throw Error(`HTTP ${r.status}: ${t.slice(0,400)}`)}return r.status===204?null:r.json()}
function showAuth(){el("auth").hidden=false;el("area").hidden=true;el("authMsg").textContent="";el("msg").textContent="";window.scrollTo({top:0,behavior:"auto"})}
function showArea(){el("auth").hidden=true;el("area").hidden=false;window.scrollTo({top:0,behavior:"auto"})}
function fail(e){el("msg").className="notice error";el("msg").textContent=e.message||String(e);el("msg").scrollIntoView({behavior:"smooth",block:"center"})}
function success(s){el("msg").className="notice ok";el("msg").textContent=s}
function toggleMode(){registro=!registro;el("nome").hidden=el("telefone").hidden=!registro;el("extraFields").hidden=!registro;el("authTitle").textContent=registro?"Criar minha conta":"Bem-vindo de volta";el("authIntro").textContent=registro?"Um cadastro rápido e você já pode cuidar do seu pet com a gente.":"Entre na sua conta para acompanhar seus pets e agendamentos.";el("authBtn").innerHTML=registro?"Criar minha conta":"Entrar na minha conta";el("toggle").textContent=registro?"Fazer login":"Criar uma conta";el("authToggleHint").textContent=registro?"Já tem cadastro?":"Ainda não tem uma conta?";el("senha").autocomplete=registro?"new-password":"current-password";el("authMsg").textContent=""}
el("toggle").onclick=toggleMode;
el("authForm").onsubmit=async e=>{e.preventDefault();el("authMsg").textContent="";el("authBtn").disabled=true;try{let email=el("email").value.trim(),senha=el("senha").value;if(registro){await request("/api/clientes/cadastro","POST",{nome:el("nome").value.trim(),telefone:el("telefone").value.trim(),email,senha});toggleMode();el("authMsg").className="notice ok";el("authMsg").textContent="Conta criada! Agora entre com o e-mail e a senha escolhidos."}else{const dados=await request("/api/clientes/login","POST",{email,senha});token=dados.token;sessionStorage.setItem("petville_demo_token",token);await init()}}catch(err){el("authMsg").className="notice error";el("authMsg").textContent=err.message}finally{el("authBtn").disabled=false}};
el("logout").onclick=()=>{token="";sessionStorage.removeItem("petville_demo_token");showAuth()};
async function init(){try{let me=await request("/api/clientes/me");el("hello").textContent="Olá, "+me.nome.split(" ")[0]+"!";showArea();await refresh()}catch(e){token="";sessionStorage.removeItem("petville_demo_token");showAuth();el("authMsg").className="notice error";el("authMsg").textContent="Entre novamente para continuar. "+e.message}}
async function refresh(){
 const [pets,servicos,agendas]=await Promise.all([request("/api/pets"),request("/api/servicos"),request("/api/agendamentos")]);
 catalogoServicos=servicos;
 el("clientPetCount").textContent=pets.length;
 el("clientAgendaCount").textContent=agendas.length;
 const future=agendas.filter(a=>new Date(a.dataHora)>new Date()&&!['Cancelado','Concluido'].includes(a.status)).sort((a,b)=>new Date(a.dataHora)-new Date(b.dataHora));
 el("clientNext").textContent=future.length?new Date(future[0].dataHora).toLocaleDateString("pt-BR"):"Nenhum";
 el("pets").innerHTML=pets.length?pets.map(p=>`<div class="pet-row"><span class="pet-avatar">${paw}</span><div><strong>${esc(p.nome)}</strong><small>${esc(p.especie)}${p.raca?' · '+esc(p.raca):''}</small></div></div>`).join(""):'<div class="empty">Você ainda não cadastrou nenhum pet.</div>';
 el("petSelect").innerHTML='<option value="">Selecione o pet</option>'+pets.map(p=>`<option value="${Number(p.id)}">${esc(p.nome)}</option>`).join("");
 el("servicoSelect").innerHTML='<option value="">Selecione o serviço</option>'+servicos.filter(s=>s.ativo).map(s=>`<option value="${Number(s.id)}">${esc(s.nome)} — ${money(s.preco)} · ${Number(s.duracaoMinutos)||60} min</option>`).join("");
 atualizarDatas();
 const tagClass={Confirmado:"confirmed",Concluido:"done",Cancelado:"cancelled"};
 el("agendas").innerHTML=agendas.length?agendas.map(a=>`<div class="agenda-row"><div><strong>${esc(a.pet)} · ${esc(a.servico)}</strong><small>${esc(friendlyDate(a.dataHora))}</small></div><span class="tag ${tagClass[a.status]||''}">${esc(a.status==='Concluido'?'Concluído':a.status)}</span></div>`).join(""):'<div class="empty">Nenhum agendamento por enquanto. Comece escolhendo um serviço!</div>';
}
el("petForm").onsubmit=async e=>{e.preventDefault();const btn=e.target.querySelector('button[type="submit"]');btn.disabled=true;try{await request("/api/pets","POST",{nome:el("petNome").value.trim(),especie:el("especie").value.trim(),raca:el("raca").value.trim(),sexo:"",dataNascimento:null});e.target.reset();await refresh();success("Pet cadastrado com sucesso!")}catch(err){fail(err)}finally{btn.disabled=false}};
function servicoAtual(){return catalogoServicos.find(s=>String(s.id)===el("servicoSelect").value);}
function atualizarDatas(){
 const campo=el("dataDia"), anterior=campo.value;
 const agora=new Date(), duracao=Number(servicoAtual()?.duracaoMinutos)||60;
 const dias=PetVilleAgenda.datasDisponiveis(agora).filter(d=>PetVilleAgenda.horariosDisponiveis(d.valor,duracao,agora).length);
 campo.innerHTML='<option value="">'+(dias.length?'Selecione um dia útil':'Nenhum dia disponível nos próximos 90 dias')+'</option>'+dias.map(d=>`<option value="${d.valor}">${esc(d.legenda)}</option>`).join("");
 if(dias.some(d=>d.valor===anterior))campo.value=anterior;
 atualizarHorarios();
}
function atualizarHorarios(){
 const campo=el("horaDia"), antigo=campo.value;
 const duracao=Number(servicoAtual()?.duracaoMinutos)||60;
 const horarios=PetVilleAgenda.horariosDisponiveis(el("dataDia").value,duracao);
 campo.innerHTML='<option value="">'+(horarios.length?'Selecione um horário':'Sem horários para essa data')+'</option>'+horarios.map(h=>`<option value="${h}">${h}</option>`).join("");
 if(horarios.includes(antigo))campo.value=antigo;
}
// Não dependa do carregamento da API para preencher as datas.
// Isso também evita que a mensagem "Carregando dias úteis..." fique presa.
try { atualizarDatas(); }
catch (erro) {
 console.error("Falha ao preparar agenda:", erro);
 el("dataDia").innerHTML = '<option value="">Não foi possível carregar os dias. Atualize a página.</option>';
 el("diasInfo").textContent = "Falha ao carregar a agenda: atualize a página (Ctrl + F5).";
}
el("servicoSelect").addEventListener("change",atualizarDatas);
el("dataDia").addEventListener("change",atualizarHorarios);
el("agendaForm").onsubmit=async e=>{
 e.preventDefault();const btn=e.target.querySelector('button[type="submit"]');btn.disabled=true;
 try{
   const data=el("dataDia").value,hora=el("horaDia").value;
   const erro=PetVilleAgenda.validar(data,hora,servicoAtual()?.duracaoMinutos||60);
   if(erro)throw Error(erro);
   await request("/api/agendamentos","POST",{petId:Number(el("petSelect").value),servicoId:Number(el("servicoSelect").value),dataHora:`${data}T${hora}:00`,observacao:el("obs").value.trim()});
   e.target.reset();await refresh();success("Agendamento solicitado com sucesso! Nosso horário: segunda a sexta, das 8h às 18h.");
 }catch(err){fail(err)}finally{btn.disabled=false}
};
if(token)init();else showAuth();
