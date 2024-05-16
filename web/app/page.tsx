import { LandingAccordion } from "@/src/components/accordion";
import Image from "next/image";

enum NoteColor {
  Green,
  Yellow,
  Blue,
  Neutral
}

export default function Home() {
  return (
    <main>
      <nav className="p-12">
        <Image className="m-auto" alt={"SotexBox Logo"} height={60} width={240} src={"/sotex_box.svg"} />
      </nav>

      <section className="p-2 flex-col justify-center items-center relative pb-40 mb-20">
        <h1 className="text-4xl text-center pt-8 pb-4 text-white font-medium leading-snug">Multimedijalna platforma koja <br /> unapredjuje Vaš biznis.</h1>
        <p className="text-center p-4 font-light text-slate-300 text-xl">Zabavan sadržaj visokog kvaliteta koji će Vam ostvariti nove klijente i povećati profit.</p>
        <div className="flex flex-row justify-center items-center gap-4 text-white pt-4 pb-16 lg:pb-32">
          <a href="#get" className="py-3 px-6 border-[1px] border-white rounded-3xl flex gap-1 justify-center">
            <Image height={20} width={20} src={"/tick.svg"} alt="Kvačica" />
            <span>Pogodnosti</span>
          </a>
          <a href="#connect" className="py-3 px-6 bg-sotex-purple-50 rounded-3xl flex gap-1 justify-center">
            <span>Kontakt</span>
            <Image height={20} width={20} src={"/arrow_right.svg"} alt="Kvačica" />
          </a>
        </div >

        <Image className="absolute bottom-0 left-0 z-[-1] w-full h-80 opacity-80" height={390} width={390} src={"/lights_1.png"} alt="Svetla" />
        <Image className="absolute bottom-0 left-0 right-0 mx-auto translate-y-2/4 h-72 sm:h-96 sm:w-[400px] md:h-96 md:w-[500px] lg:w-[960px] lg:h-[550px]" height={1200} width={1200} src={"/tv.png"} alt="Svetla" />
      </section>

      <section className="pt-24 lg:pt-36 relative">
        <h2 className="text-white pt-8 pb-4 text-5xl font-medium text-center m-auto" id="get">Šta dobijate</h2>

        <div className="p-4 pb-32 lg:grid lg:grid-cols-3 lg:gap-8 max-w-5xl m-auto w-fit">
          <FeatureElement header="Uredjaj za TV" icon="/icons/router.svg" paragraph="Besplatan uredjaj za TV i besplatno korišćenje." note="Besplatno" noteColor={NoteColor.Yellow} />
          <FeatureElement header="Postavite reklamu" icon="/icons/cloud.svg" paragraph="Postavite Vaše reklame na uredjaj kroz jednostavan korisnički interfejs." />
          <FeatureElement header="Aplikacija" icon="/icons/mobile.svg" paragraph="Obezbedjujemo vam jednostavan korisnički interfejs." note="App" noteColor={NoteColor.Blue} />
          <FeatureElement header="Kanal za restorane" icon="/icons/food.svg" paragraph="Zabavan sadržaj visokog kvaliteta u Full HD Rezoluciji." note="Novi su u izradi!" noteColor={NoteColor.Green} />
          <FeatureElement header="Full HD 1080p" icon="/icons/hd.svg" paragraph="Zabavan sadržaj visokog kvaliteta u Full HD Rezoluciji." />
          <FeatureElement header="Full HD 1080p" icon="/icons/hd.svg" paragraph="Zabavan sadržaj visokog kvaliteta u Full HD Rezoluciji." />
        </div>
      </section>

      <section className="rounded-t-2xl bg-sotex-black-50 px-4 pt-12 pb-24">
        <h2 className="text-white pt-8 pb-4 text-5xl font-medium text-center m-auto whitespace-nowrap" id="connect">Povežimo se</h2>
        <p className="text-center p-4 font-light text-slate-300 text-xl">Ostavite nam svoj broj telefona i/ili email adresu i kontaktiraćemo Vas u najkraćem mogućem roku</p>
        <form method="POST" className="flex flex-col items-center justify-center w-full gap-4">
          <div className="flex flex-col gap-4 lg:bg-white lg:px-12 lg:rounded-3xl lg:flex-row lg:py-2 lg:gap-2">
            <input type="email" placeholder="Unesite email adresu." className="p-4 rounded-3xl outline-none" />
            <div className="border-l-2 border-l-gray-200 hidden lg:block"></div>
            <input type="tel" placeholder="Unesite telefon." className="p-4 rounded-3xl outline-none" />
            <button type="button" className="bg-sotex-purple-50 text-white flex-row justify-center items-center gap-2 py-4 px-10 w-full rounded-3xl hidden lg:flex">
              <span>Prosledi</span>
              <Image height={20} width={20} src={"/arrow_right.svg"} alt="Kvačica" />
            </button>
            <button type="button" className="bg-sotex-purple-50 text-white flex-row flex justify-center items-center gap-2 p-4 rounded-3xl lg:hidden">
              <span>Prosledi</span>
              <Image height={20} width={20} src={"/arrow_right.svg"} alt="Kvačica" />
            </button>
          </div>

        </form>
      </section>

      <section className="p-4 lg:pt-12 lg:pb-24">
        <h2 className="text-white pt-8 pb-8 text-5xl font-medium text-center m-auto" id="requirements">Uslovi korišćenja</h2>

        <div className="lg:flex flex-row justify-center items-start gap-8 max-w-5xl m-auto">
          <div className="lg:flex flex-col flex-1 gap-8 w-fit m-auto">
            <div></div>
            <FeatureElement header="Postavite reklamu" icon="/icons/play.svg" paragraph="Postavite Vaše reklame na uredjaj kroz jednostavan korisnički interfejs." note="10 min/h" noteColor={NoteColor.Green} />
            <FeatureElement header="Postavite reklamu" icon="/icons/live.svg" paragraph="Postavite Vaše reklame na uredjaj kroz jednostavan korisnički interfejs." note="6/24 h" noteColor={NoteColor.Yellow} />
          </div>

          <Image className="py-8 flex-1 lg:py-0 m-auto" height={400} width={400} src={"/splash_1.png"} alt="Box slika." />
        </div>
      </section>

      <LandingAccordion></LandingAccordion>

      <footer className="p-4 relative flex flex-col justify-center items-center py-12">
        <div className="max-w-5xl m-auto flex flex-col justify-center items-center w-full lg:flex-row lg:justify-between lg:items-start">
          <div className="flex flex-col items-start justify-center">
            <Image className="" src={"/sotex_box.svg"} height={24} width={264} alt="SotexBox logo." />
            <div className="flex flex-row gap-2 justify-start items-center py-8">
              <span className="text-lg whitespace-nowrap text-slate-300">Omogućeno od</span>
              <Image className="h-12 w-24" src={"/sotex_solutions.svg"} height={20} width={264} alt="Sotex Solutions logo." />
            </div>
            <Image className="py-8 hidden lg:block" height={300} width={300} src={"/splash_2.png"} alt="Box slika." />
            <span className="text-slate-300 whitespace-nowrap m-auto w-fit hidden lg:inline-block">© 2024 SotexBOX. Sva prava zadržana.</span>
          </div>


          <div className="flex justify-start items-start gap-4 flex-col lg:flex-row lg:justify-center lg:items-center">
            <div className="text-slate-300 flex flex-col w-full">
              <a href="#get">Šta dobijate</a>
              <a href="#requirements">Uslovi korišćenja</a>
            </div>

            <div className="text-slate-300 flex flex-col items-start justify-start w-full py-4">
              <span className="text-lg whitespace-nowrap text-slate-300">Novi Sad, Srbija</span>
              <a href="mailto:contact@sotexsolutions.com">contact@sotexsolutions.com</a>
              <a href="tel:+381-64-165-7193">+381-64-165-7193</a>
            </div>
          </div>
        </div>

        <Image className="py-8 lg:hidden" height={300} width={300} src={"/splash_2.png"} alt="Box slika." />
        <span className="text-slate-300 whitespace-nowrap m-auto w-fit lg:hidden">© 2024 SotexBOX. Sva prava zadržana.</span>

        <Image className="absolute bottom-0 left-0 z-[-1] w-full h-80 opacity-80" height={390} width={390} src={"/lights_2.png"} alt="Svetla" />

      </footer>
    </main>
  );


  function FeatureElement({ header, paragraph, icon, note, noteColor = NoteColor.Neutral }: { header: string, paragraph: string, icon: string, note?: string, noteColor?: NoteColor }) {
    const colors: { [color: string]: string } = {
      [NoteColor.Green]: "bg-sotex-green-50",
      [NoteColor.Yellow]: "bg-sotex-yellow-50",
      [NoteColor.Blue]: "bg-blue-500",
      [NoteColor.Neutral]: "bg-white"
    };
    return (
      <div className="flex flex-col justify-start max-w-lg items-start p-4 gap-4 border-[1px] border-b-0 border-sotex-black-450 last:border-b-[1px] first:rounded-t-2xl lg:border-b-[1px] lg:flex-1 lg:rounded-2xl hover:shadow-lg hover:shadow-neutral-900 transition-all ease-in-out duration-500">
        <div className="flex flex-row justify-between items-start w-full">
          <Image className="h-14 w-14" src={icon} alt={header} height={56} width={56} />
          {note ?
            <div className={`${colors[noteColor.toString()]} px-2 py-1 rounded-lg font-semibold`}>{note}</div> : ""}
        </div>
        <span className="text-white text-2xl font-medium whitespace-nowrap">{header}</span>
        <p className="text-slate-300">{paragraph}</p>
      </div>
    );
  }
}
