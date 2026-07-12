(function(){
  'use strict';

  const RESOURCES={catalog:'store-items',wallets:'wallets',inventory:'inventory',purchases:'purchases',identities:'visual-identities'};
  const ITEM_TYPES=new Set(['Avatar','ProfileBackground','Frame','Effect','Title','Badge','Emblem','EmblemBackground','TeamColor','TeamBanner','TeamEffect','TeamLivingEmblem']);
  const TEAM_TYPES=new Set(['Emblem','EmblemBackground','TeamColor','TeamBanner','TeamEffect','TeamLivingEmblem']);
  const unwrap=row=>row?.payload??row;
  const utcNow=()=>new Date().toISOString();

  function normalizeWallet(playerId,row){const value=unwrap(row)||{};return{playerId:String(value.playerId||playerId||''),coins:Number.isFinite(Number(value.coins))?Math.max(0,Math.trunc(Number(value.coins))):0,gems:Number.isFinite(Number(value.gems))?Math.max(0,Math.trunc(Number(value.gems))):0,createdAt:value.createdAt||utcNow(),updatedAt:value.updatedAt||utcNow()}}
  function normalizeItem(row){const value=unwrap(row)||{},assetId=String(value.assetId||value.itemId||value.id||'').trim(),itemType=String(value.storeTypeId||value.itemType||value.type||'').trim();return{...value,assetId,itemId:assetId,itemType,storeTypeId:itemType,title:String(value.title||value.name||assetId),description:String(value.description||''),coinPrice:Math.max(0,Math.trunc(Number(value.coinPrice??value.coinsPrice??0)||0)),gemPrice:Math.max(0,Math.trunc(Number(value.gemPrice??value.gemsPrice??0)||0)),isPublished:value.isPublished!==false,isFree:Boolean(value.isFree)||(!Number(value.coinPrice)&&!Number(value.gemPrice)),previewAsset:String(value.previewAsset||value.imagePath||value.assetPath||'')}}
  function normalizeOwned(playerId,row){const value=unwrap(row)||{},assetId=String(value.assetId||value.itemId||'').trim(),itemType=String(value.storeTypeId||value.itemType||'').trim();return{...value,playerId:String(value.playerId||playerId||''),assetId,itemId:assetId,itemType,storeTypeId:itemType,acquiredAt:value.acquiredAt||value.purchasedAt||utcNow(),isEquipped:Boolean(value.isEquipped)}}
  function assertPlayer(playerId){if(!String(playerId||'').trim())throw new Error('لا يوجد PlayerId صالح للحساب الحالي.')}
  function assertItem(item){if(!item?.assetId)throw new Error('معرّف عنصر المتجر مفقود.');if(!ITEM_TYPES.has(item.itemType))throw new Error(`نوع عنصر غير مدعوم: ${item.itemType||'غير محدد'}`)}

  function create({api,putResource,loadResource}){
    if(typeof api!=='function'||typeof putResource!=='function'||typeof loadResource!=='function')throw new Error('StoreDomain requires api, putResource, and loadResource.');
    async function loadCatalog(){return(await loadResource(RESOURCES.catalog)).map(normalizeItem).filter(x=>x.assetId&&x.isPublished&&ITEM_TYPES.has(x.itemType))}
    async function getWallet(playerId){assertPlayer(playerId);const rows=await loadResource(RESOURCES.wallets),existing=rows.map(unwrap).find(x=>String(x?.playerId||'')===String(playerId));return normalizeWallet(playerId,existing)}
    async function saveWallet(wallet){assertPlayer(wallet?.playerId);const normalized=normalizeWallet(wallet.playerId,{...wallet,updatedAt:utcNow()});await putResource(RESOURCES.wallets,normalized.playerId,normalized);return normalized}
    async function getInventory(playerId){assertPlayer(playerId);return(await loadResource(RESOURCES.inventory)).map(x=>normalizeOwned(playerId,x)).filter(x=>x.playerId===playerId&&x.assetId)}
    async function getPurchases(playerId){assertPlayer(playerId);return(await loadResource(RESOURCES.purchases)).map(unwrap).filter(x=>String(x?.playerId||'')===String(playerId))}
    async function getIdentities(playerId){assertPlayer(playerId);return(await loadResource(RESOURCES.identities)).map(unwrap).filter(x=>String(x?.playerId||'')===String(playerId))}

    async function acquire(playerId,item){assertPlayer(playerId);assertItem(item);const inventory=await getInventory(playerId);if(inventory.some(x=>x.assetId===item.assetId))return{ok:true,alreadyOwned:true,item};const wallet=await getWallet(playerId);if(wallet.coins<item.coinPrice)throw new Error('رصيد العملات غير كافٍ.');if(wallet.gems<item.gemPrice)throw new Error('رصيد الجواهر غير كافٍ.');wallet.coins-=item.coinPrice;wallet.gems-=item.gemPrice;await saveWallet(wallet);const purchaseId=`PUR-${crypto.randomUUID().replaceAll('-','').toUpperCase()}`,purchase={purchaseId,playerId,assetId:item.assetId,storeTypeId:item.itemType,coinPrice:item.coinPrice,gemPrice:item.gemPrice,purchasedAt:utcNow(),status:'Completed'},owned={playerId,assetId:item.assetId,storeTypeId:item.itemType,acquiredAt:purchase.purchasedAt,isEquipped:false,source:'StorePurchase'};await putResource(RESOURCES.purchases,purchaseId,purchase);await putResource(RESOURCES.inventory,`${playerId}:${item.itemType}:${item.assetId}`,owned);return{ok:true,alreadyOwned:false,item,purchase,wallet}}

    async function equip(playerId,item,target={}){
      assertPlayer(playerId);assertItem(item);
      const inventory=await getInventory(playerId),owned=inventory.find(x=>x.assetId===item.assetId&&x.itemType===item.itemType);
      if(!owned)throw new Error('يجب امتلاك العنصر قبل تجهيزه.');
      const isTeam=TEAM_TYPES.has(item.itemType),teamId=String(target.teamId||'').trim();
      if(isTeam&&!teamId)throw new Error('اختر الفريق الذي تريد تجهيز العنصر له.');
      if(!isTeam){for(const entry of inventory.filter(x=>x.itemType===item.itemType)){const next={...entry,isEquipped:entry.assetId===item.assetId,equippedAt:entry.assetId===item.assetId?utcNow():entry.equippedAt||null};await putResource(RESOURCES.inventory,`${playerId}:${entry.itemType}:${entry.assetId}`,next)}}
      const identity={playerId,ownerScope:isTeam?'Team':'Player',teamId:isTeam?teamId:'',targetId:isTeam?teamId:playerId,storeTypeId:item.itemType,itemType:item.itemType,assetId:item.assetId,updatedAt:utcNow()};
      const identityId=isTeam?`${playerId}:Team:${teamId}:${item.itemType}`:`${playerId}:Player:${item.itemType}`;
      await putResource(RESOURCES.identities,identityId,identity);
      return identity;
    }

    async function getEquipped(playerId,itemType,target={}){assertPlayer(playerId);const identities=await getIdentities(playerId),teamId=String(target.teamId||'');const identity=identities.find(x=>String(x.storeTypeId||x.itemType)===itemType&&(TEAM_TYPES.has(itemType)?String(x.teamId||x.targetId)===teamId:String(x.ownerScope||'Player')!=='Team'));if(!identity)return null;const inventory=await getInventory(playerId);return inventory.find(x=>x.itemType===itemType&&x.assetId===identity.assetId)||null}
    return{loadCatalog,getWallet,saveWallet,getInventory,getPurchases,getIdentities,acquire,equip,getEquipped,ITEM_TYPES:[...ITEM_TYPES],TEAM_TYPES:[...TEAM_TYPES]};
  }

  window.DominoStoreDomain={create,RESOURCES,ITEM_TYPES:[...ITEM_TYPES],TEAM_TYPES:[...TEAM_TYPES]};
})();