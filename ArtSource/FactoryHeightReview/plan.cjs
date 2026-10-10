const fs = require('fs');
const dir = 'ArtSource/FactoryHeightReview/';
const rows = JSON.parse(fs.readFileSync(dir+'survey.json'));
const supports = JSON.parse(fs.readFileSync(dir+'supports.json'));
const attachments = JSON.parse(fs.readFileSync(dir+'attachments.json'));
const byId = new Map(rows.map(r=>[r.id,r]));
const parents = new Map(rows.map(r=>[r.id,r.id]));
const edges = [];
const building = r=>r.path.includes('厂房');
const floor = r=>r.name.includes('ModulFloor');
function find(id) { let p=parents.get(id); if(p!==id){p=find(p);parents.set(id,p);} return p; }
function join(a,b,reason) {parents.set(find(a),find(b));edges.push({a,b,reason});}
join(83974,-113074,'building on modular foundation');
join(82478,-47758,'building on modular foundation');
join(-47758,-29778,'coincident existing modular foundations');
const attachmentIds = new Set();
for(const a of attachments) {
    if(a.name.includes('Antena'))continue;
    const matches=a.matches.filter(m=>(Number.isFinite(m.down)&&m.down<2)||(Number.isFinite(m.side)&&m.side<1));
    matches.sort((a,b)=>Math.min(Math.abs(+a.down),+a.side)-Math.min(Math.abs(+b.down),+b.side));
    if(matches.length!==1)throw Error('Unresolved attachment '+a.id);
    join(a.id,matches[0].id,'verified roof or wall mesh');attachmentIds.add(a.id);
}
join(-82820,83374,'roof antenna confirmed in Scene View');attachmentIds.add(-82820);
join(-76276,-126020,'guard post furniture');
for(const [a,b] of [[-121914,82478],[-120000,82478],[-139190,83974]]) {join(a,b,'entrance aligned with building foundation');attachmentIds.add(a);}
const fixed = new Set(rows.filter(r=>building(r)||floor(r)||attachmentIds.has(r.id)||r.id===-126020||r.id===-76276||r.id===-120330).map(r=>r.id));
const anchors=[];
for(const q of supports) {
    const a=byId.get(q.id);
    if(building(a)||floor(a)||attachmentIds.has(a.id)||a.id===-76276)continue;
    let candidates=q.supports.filter(s=>{
        const b=byId.get(s.id);
        if(building(b)&&a.path.includes('金属围栏'))return false;
        if(building(b)&&a.min[1]<b.min[1]+.3)return false;
        return s.gap>=-.28&&s.gap<=.32;
    });
    if(!candidates.length)continue;
    const best=Math.min(...candidates.map(s=>Math.abs(s.gap)));
    candidates=candidates.filter(s=>Math.abs(s.gap)<=best+.08);
    for(const s of candidates) {
        if(fixed.has(s.id))anchors.push({a:a.id,b:s.id,reason:'rests on structural surface'});
        else join(a.id,s.id,'mesh support and existing stack');
    }
}
const groups=new Map();
for(const r of rows){const id=find(r.id);if(!groups.has(id))groups.set(id,[]);groups.get(id).push(r);}
const plan=[];
const groupDeltas=new Map();
const ordered=[...groups.values()].sort((a,b)=>Number(b.some(r=>fixed.has(r.id)))-Number(a.some(r=>fixed.has(r.id))));
for(const members of ordered) {
    const buildings=members.filter(building);
    if(buildings.length>1)throw Error('Assembly connects multiple buildings '+buildings.map(r=>r.id));
    const structures=members.filter(r=>building(r)||floor(r)||r.id===-126020||r.id===-120330);
    let delta;
    if(structures.length)delta=Math.min(...structures.map(r=>r.footprintMin!==0?r.footprintMin-r.min[1]:Math.min(...r.contacts.map(c=>c.offset))))-.02;
    else {
        const on=anchors.filter(a=>members.some(r=>r.id===a.a));
        delta=Math.max(Math.max(...members.map(r=>r.clearDelta))-.01,...on.map(a=>groupDeltas.get(find(a.b))));
    }
    groupDeltas.set(find(members[0].id),delta);
    for(const r of members) {
        const correction=r.id===83582?-1.34765613:0;
        plan.push({id:r.id,name:r.name,path:r.path,members:r.members,globalId:r.globalId,before:r.position,deltaY:delta+correction,assembly:members[0].id,policy:structures.length?'lowest terrain, uphill embedding permitted':'lower mesh envelope or existing structural support, whichever is higher',reason:[...edges,...anchors].filter(e=>e.a===r.id||e.b===r.id)});
    }
}
fs.writeFileSync(dir+'plan.json',JSON.stringify(plan,null,2));
fs.writeFileSync(dir+'relationships.json',JSON.stringify([...edges,...anchors],null,2));
console.log(JSON.stringify({objects:plan.length,assemblies:groups.size,buildingAssemblies:[...groups.values()].filter(g=>g.some(building)).length,attachments:attachmentIds.size,minDelta:Math.min(...plan.map(r=>r.deltaY)),maxDelta:Math.max(...plan.map(r=>r.deltaY)),largest:[...groups.values()].sort((a,b)=>b.length-a.length).slice(0,8).map(g=>({count:g.length,buildings:g.filter(building).map(x=>x.name)}))},null,2));
