"use strict";
const el = id => document.getElementById(id);
const esc = v => String(v ?? "").replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const money = v => Number(v || 0).toLocaleString("pt-BR", {style:"currency",currency:"BRL"});
const time = v => { const d = new Date(v); return Number.isNaN(d.valueOf()) ? "—" : d.toLocaleString("pt-BR",{dateStyle:"short",timeStyle:"short"}); };
const empty = (text) => `<div class="empty"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="12" r="9"/><path d="M8 12h8"/></svg><div>${esc(text)}</div></div>`;
const loading = `<div class="empty">Carregando informações...</div>`;
const table = (columns, rows) => `<table><thead><tr>${columns.map(x => `<th>${x}</th>`).join("")}</tr></thead><tbody>${rows.join("")}</tbody></table>`;
async function api(path,opts={}) {
 const headers = {"Content-Type":"application/json", "X-Admin-Key":el("key").value, ...(opts.headers||{})};
 const r = await fetch("/api/admin/"+path,{...opts,headers});
 if (!r.ok) {
  const raw = await r.text(); let message=raw;
  try {const data=JSON.parse(raw);message=data.mensagem||data.title||raw;}catch{}
  throw Error(`HTTP ${r.status} — ${message.slice(0,350)}`);
 }
 return r.status === 204 ? null : r.json();
}
const statuses = ["Pendente","Confirmado","Concluido","Cancelado"];
const fullStatus = {Pendente:"Pendente",Confirmado:"Confirmado",Concluido:"Concluído",Cancelado:"Cancelado"};
async function carregar(){
 const ids=["agendas","clientes","produtos"];
 ids.forEach(id=>el(id).innerHTML=loading);
 el("msg").textContent="";
 try{
 const [s,a,c,p]=await Promise.all([api("resumo"),api("agendamentos"),api("clientes"),api("produtos")]);
 el("painel").hidden=false;
 ["clientes","pets","agendamentos","produtos"].forEach(k=>el("n"+k).textContent=Number(s[k]??0).toLocaleString("pt-BR"));
 el("agendaCount").textContent=`${a.length} ${a.length===1?"registro":"registros"}`;
 el("clientesCount").textContent=`${c.length} ${c.length===1?"cliente":"clientes"}`;
 el("produtosCount").textContent=`${p.length} ${p.length===1?"produto":"produtos"}`;
 el("clientes").innerHTML=c.length?table(["Cliente","E-mail","Telefone"],c.map(x=>`<tr><td><div class="person"><span class="initial">${esc(String(x.nome||'?').trim().charAt(0).toUpperCase())}</span>${esc(x.nome)}</div></td><td>${esc(x.email)}</td><td>${esc(x.telefone||'—')}</td></tr>`)):empty("Nenhum cliente cadastrado ainda.");
 el("agendas").innerHTML=a.length?table(["Data e horário","Cliente","Pet","Serviço","Situação"],a.map(x=>`<tr><td>${esc(time(x.dataHora))}</td><td>${esc(x.cliente)}</td><td><strong>${esc(x.pet)}</strong></td><td>${esc(x.servico)}</td><td><select data-id="${Number(x.id)}" class="status-select" aria-label="Alterar status do agendamento">${statuses.map(v=>`<option value="${v}" ${x.status===v?'selected':''}>${fullStatus[v]}</option>`).join("")}</select></td></tr>`)):empty("Nenhum agendamento registrado ainda.");
 el("produtos").innerHTML=p.length?table(["Produto","Categoria","Valor","Em estoque","Ação"],p.map(x=>`<tr><td><strong>${esc(x.nome)}</strong></td><td>${esc(x.categoria)}</td><td>${esc(money(x.preco))}</td><td>${Number(x.quantidadeEstoque)}</td><td><button class="btn danger sm remove-product" data-id="${Number(x.id)}" type="button">Excluir</button></td></tr>`)):empty("Nenhum produto no estoque. Cadastre o primeiro acima.");
 }catch(e){el("painel").hidden=true;el("msg").textContent="Não foi possível carregar os dados. "+e.message;}
}
el("agendas").addEventListener("change",async e=>{
 const select=e.target.closest(".status-select");if(!select)return;
 select.disabled=true;
 try{await api("agendamentos/"+Number(select.dataset.id)+"/status",{method:"PUT",body:JSON.stringify({status:select.value})});await carregar();}
 catch(err){alert("Não foi possível alterar: "+err.message);await carregar();}
 finally{select.disabled=false;}
});
el("produtos").addEventListener("click",async e=>{
 const btn=e.target.closest(".remove-product");if(!btn||!confirm("Deseja excluir este produto?"))return;
 btn.disabled=true;
 try{await api("produtos/"+Number(btn.dataset.id),{method:"DELETE"});await carregar();}catch(err){alert("Falha ao excluir: "+err.message);btn.disabled=false;}
});
el("produtoform").addEventListener("submit",async e=>{
 e.preventDefault();const button=e.target.querySelector('button[type="submit"]');button.disabled=true;
 try{await api("produtos",{method:"POST",body:JSON.stringify({nome:el("pnome").value.trim(),categoria:el("pcategoria").value.trim(),preco:Number(el("ppreco").value),quantidadeEstoque:Number(el("pquantidade").value),descricao:""})});e.target.reset();await carregar();}
 catch(err){alert("Não foi possível cadastrar: "+err.message);}finally{button.disabled=false;}
});
el("refresh").onclick=carregar;
el("key").addEventListener("change",carregar);
const currentDate=new Intl.DateTimeFormat("pt-BR",{weekday:"long",day:"numeric",month:"long",year:"numeric"}).format(new Date());
el("today").textContent=currentDate.charAt(0).toUpperCase()+currentDate.slice(1);
document.querySelectorAll('[data-nav]').forEach(link=>link.addEventListener('click',()=>{document.querySelectorAll('[data-nav]').forEach(x=>x.classList.remove('active'));link.classList.add('active');}));
(async()=>{try{const r=await fetch("/api/health/database");if(!r.ok)throw Error("offline");const s=await r.json();el("dbStatus").textContent=`${s.tipo||"Banco"} • conectado`;}catch{el("dbStatus").textContent="Banco indisponível";el("dbStatus").classList.add("off");}})();
carregar();
