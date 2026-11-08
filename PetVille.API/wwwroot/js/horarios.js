"use strict";
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
