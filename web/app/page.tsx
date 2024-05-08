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
      <nav className="p-4">
        <Image className="m-auto" alt={"SotexBox Logo"} height={60} width={240} src={"/sotex_box.svg"} />
      </nav>

      <section className="p-2 flex-col justify-center items-center relative pb-28 mb-20">
        <h1 className="text-5xl text-center pt-8 pb-4 text-white font-medium leading-snug">SotexBox <br /> TV platforma unapredjuje Vaš biznis.</h1>
        <p className="text-center p-4 font-light text-slate-300 text-xl">Zabavan sadržaj visokog kvaliteta koji će Vam ostvariti nove klijente i povećati profit.</p>
        <div className="flex flex-row justify-center items-center gap-4 text-white py-4">
          <a className="py-3 px-6 border-[1px] border-white rounded-3xl flex gap-1 justify-center">
            <Image height={20} width={20} src={"/tick.svg"} alt="Kvačica" />
            <span>Pogodnosti</span>
          </a>
          <a className="py-3 px-6 bg-fuchsia-700 rounded-3xl flex gap-1 justify-center">
            <span>Kontakt</span>
            <Image height={20} width={20} src={"/arrow_right.svg"} alt="Kvačica" />
          </a>

        </div >
        <Image className="absolute bottom-0 left-0 z-[-1] w-full h-80 opacity-80" height={48} width={320} src={"/lights_1.png"} alt="Svetla" />

        <Image className="absolute bottom-0 left-0 translate-y-2/4 w-full h-72" height={48} width={320} src={"/tv.png"} alt="Svetla" />
      </section>

      <section className="pt-24 relative">
        <h2 className="text-white pt-8 pb-4 text-5xl font-medium text-center m-auto">Šta dobijate</h2>

        <div className="p-4">
          <FeatureElement header="Full HD 1080p" icon="/icons/hd.svg" paragraph="Zabavan sadržaj visokog kvaliteta u Full HD Rezoluciji." />
          <FeatureElement header="Kanal za restorane" icon="/icons/food.svg" paragraph="Zabavan sadržaj visokog kvaliteta u Full HD Rezoluciji." note="Novi su u izradi!" noteColor={NoteColor.Green} />
          <FeatureElement header="Uredjaj za TV" icon="/icons/router.svg" paragraph="Besplatan uredjaj za TV i besplatno korišćenje." note="Besplatno" noteColor={NoteColor.Yellow} />
          <FeatureElement header="Aplikacija" icon="/icons/mobile.svg" paragraph="Obezbedjujemo vam jednostavan korisnički interfejs." note="App" noteColor={NoteColor.Blue} />
          <FeatureElement header="Postavite reklamu" icon="/icons/cloud.svg" paragraph="Postavite Vaše reklame na uredjaj kroz jednostavan korisnički interfejs." />
          <FeatureElement header="Uslovi korišćenja" icon="/icons/document.svg" paragraph="Saznaj više." />
        </div>
      </section>

      <section className="rounded-t-2xl bg-neutral-700 p-4">
        <h2 className="text-white pt-8 pb-4 text-5xl font-medium text-center m-auto whitespace-nowrap">Povežimo se</h2>
        <p className="text-center p-4 font-light text-slate-300 text-xl">Ostavite nam svoj broj telefona i/ili email adresu i kontaktiraćemo Vas u najkraćem mogućem roku</p>
        <form method="POST" className="flex flex-col items-center justify-center w-full gap-4">
          <input type="email" placeholder="Unesite email adresu." className="p-4 rounded-3xl" />
          <input type="tel" placeholder="Unesite telefon." className="p-4 rounded-3xl" />

          <button type="button" className="bg-fuchsia-700 text-white flex-row flex justify-center items-center gap-2 p-4 w-full rounded-3xl">
            <span>Prosledi</span>
            <Image height={20} width={20} src={"/arrow_right.svg"} alt="Kvačica" />
          </button>
        </form>
      </section>

      <section className="p-4">
        <h2 className="text-white pt-8 pb-8 text-5xl font-medium text-center m-auto">Uslovi korišćenja</h2>
        <FeatureElement header="Postavite reklamu" icon="/icons/play.svg" paragraph="Postavite Vaše reklame na uredjaj kroz jednostavan korisnički interfejs." note="10 min/h" noteColor={NoteColor.Green} />
        <FeatureElement header="Postavite reklamu" icon="/icons/live.svg" paragraph="Postavite Vaše reklame na uredjaj kroz jednostavan korisnički interfejs." note="6/24 h" noteColor={NoteColor.Yellow} />
      </section>

    </main>

  );


  function FeatureElement({ header, paragraph, icon, note, noteColor = NoteColor.Neutral }: { header: string, paragraph: string, icon: string, note?: string, noteColor?: NoteColor }) {
    const colors: { [color: string]: string } = {
      [NoteColor.Green]: "bg-green-500",
      [NoteColor.Yellow]: "bg-yellow-500",
      [NoteColor.Blue]: "bg-blue-500",
      [NoteColor.Neutral]: "bg-white"
    };
    return (
      <div className="flex flex-col justify-start items-start p-4 gap-4 border-[1px] border-b-0 border-slate-300 last:border-b-[1px] first:rounded-t-2xl">
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
