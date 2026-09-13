// Headless browser validation of the delivered local review; no desktop UI control.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const {chromium}=require('C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const out=path.join(__dirname,'output/playwright');fs.mkdirSync(out,{recursive:true});
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1080},deviceScaleFactor:1});const errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400&&!r.url().endsWith('favicon.ico'))errors.push(r.status()+' '+r.url());});
  await page.goto('http://127.0.0.1:18273/analysis/review.html');
  await page.waitForFunction(()=>Array.from(document.images).every(i=>i.complete&&i.naturalWidth>0));
  const tabs=page.getByRole('tab');const count=await tabs.count();if(count!==5)throw Error('Expected 5 candidate tabs');
  const animated=[];
  for(let i=0;i<count;i++){
   await tabs.nth(i).click();const panel=page.getByRole('tabpanel');if(await panel.count()!==1)throw Error('Wrong visible panel count');
   const motion=panel.locator('.motion');const first=await motion.screenshot();await page.waitForTimeout(800);const second=await motion.screenshot();
   const changed=!first.equals(second);animated.push({id:await panel.locator('h2').textContent(),changed});if(!changed)throw Error('Source idle did not visibly advance');
  }
  await tabs.nth(0).click();await page.screenshot({path:path.join(out,'review-desktop.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);if(overflow)throw Error('Mobile overflow');
  await page.screenshot({path:path.join(out,'review-mobile.png'),fullPage:true});
  const result={ok:errors.length===0,errors,candidateTabs:count,animated,mobileOverflow:overflow,checkedAt:new Date().toISOString()};
  fs.writeFileSync(path.join(out,'browser-validation.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));if(errors.length)process.exitCode=1;
 }finally{await browser.close();}
})().catch(e=>{console.error(e.stack);process.exitCode=1;});
