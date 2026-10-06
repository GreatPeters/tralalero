using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Localization.Settings;

// Presentation-only translations for the two locales already registered by the
// project. Authored Korean gameplay identifiers and reward calculations stay intact.
public static class Chapter45PresentationText
{
    public static bool English => LocalizationSettings.SelectedLocale != null
        && LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> EnglishText = new()
    {
        // Encounter-entry and committed-choice notices reproduced in actual runs.
        ["경비 구역"] = "Guard area",
        ["놀란 손님들이 휴대폰을 꺼냅니다"] = "Startled shoppers take out their phones",
        ["낯선 전도자가 손을 뻗습니다"] = "A preacher reaches for you",
        ["향수 샘플과 명함을 피하세요"] = "Dodge perfume samples and flyers",
        ["사람이 많은 쇼핑 골목"] = "A crowded shopping lane",
        ["차량 많은 길 · 빠르게 통과하세요"] = "Heavy traffic · Pass through quickly",
        ["앞길의 경비를 정리하자"] = "Clear the guards ahead",
        ["방패 · 다음 피해 1회 방어"] = "Shield · Block the next hit",
        ["공격력 +20%"] = "ATK +20%",
        ["관광객"] = "Tourists",
        ["가드"] = "Guards",
        ["의류 매장"] = "Clothing shops",
        ["쇼핑백 여성"] = "Shoppers with bags",
        ["커플"] = "Couples",
        ["스포츠 고객"] = "Sports shoppers",
        ["농구 중보스"] = "Basketball miniboss",
        ["학생"] = "Students",
        ["리빙 고객"] = "Homeware shoppers",
        ["접시 투척"] = "Plate throwers",
        ["TV 코너"] = "TV corner",
        ["혼합 스포츠"] = "Sports crowd",
        ["식당 대기줄"] = "Food court queue",
        ["혼합 손님"] = "Mixed crowd",
        ["배식 출구"] = "Serving area exit",
        ["매표소 커플"] = "Couples at the ticket office",
        ["영화관 아이들"] = "Children at the cinema",
        ["직원과 청소부"] = "Staff and cleaners",
        ["상영관 입장 대기"] = "Queue for the screening room",
        ["저가 매장 매니저"] = "Budget shop managers",
        ["전문 매장 매니저"] = "Specialty shop managers",
        ["최종 전시 매니저"] = "Final display managers",
        ["보안문 개방"] = "Security door open",
        ["열린 통로로 이동하세요"] = "Move through the open passage",
        ["길을 선택하세요"] = "Choose your route",
        ["선택한 길로 이동합니다"] = "Following your chosen route",
        ["보상 획득"] = "Reward received",
        ["행동 예고"] = "Action warning",
        ["위험 예고"] = "Danger ahead",
        ["보호막 사용"] = "Shield used",
        ["공격을 한 번 막았습니다"] = "Blocked one hit",
        ["잠실 · 하늘의 신발"] = "Jamsil · Shoes in the sky",
        ["슈 타워 · 최상층으로"] = "Shoe Tower · To the top",
        ["신발이 있는 탑으로 향하세요"] = "Head for the tower with the shoes",
        ["매장을 지나 최상층의 운동화를 찾으세요"] = "Pass the shops and find the sneakers upstairs",
        ["에스컬레이터 이동"] = "Taking the escalator",
        ["엘리베이터 이동"] = "Taking the elevator",
        ["에스컬레이터 준비"] = "Escalator ready soon",
        ["엘리베이터 준비"] = "Elevator ready soon",
        ["문이 열렸습니다 · 앞으로 이동하세요"] = "Doors open · Move forward",
        ["호출된 대기줄이 이동합니다"] = "The called queue is moving",
        ["공물을 찾았다!"] = "Offering found!",
        ["더 좋은 신발을 손에 넣었습니다"] = "You found better sneakers",
        ["돌아가기"] = "Return",
        ["1F 명품 · LUXURY"] = "1F LUXURY",
        ["2F 의류 · FASHION"] = "2F FASHION",
        ["3F 스포츠 · SPORTS"] = "3F SPORTS",
        ["4F 리빙 · LIVING"] = "4F LIVING",
        ["B1 식당가 · FOOD COURT"] = "B1 FOOD COURT",
        ["5F 영화관 · TICKETS"] = "5F CINEMA TICKETS",
        ["6F 상영관 · SCREEN"] = "6F SCREENING ROOM",
        ["7F 신발 · SHOE GALLERY"] = "7F SHOE GALLERY",
        ["시향지와 명함을 뿌립니다 · 빈 틈으로 피하세요"] = "Flyers incoming · Dodge through a gap",
        ["예수를 믿으세요! · 뻗는 손을 피하세요"] = "Believe in Jesus! · Dodge the reaching hand",
        ["촬영하는 손님 사이를 지나가세요"] = "Pass between the people filming",
        ["골프채 스윙 · 팔 바깥으로 피하세요"] = "Golf swing · Stay outside the arm's reach",
        ["다가오는 사람을 피하세요"] = "Dodge the approaching people",
        ["대걸레를 휘두릅니다"] = "Mop swing incoming",
        ["돌파 준비 · 표시된 방향을 피하세요"] = "Charge incoming · Avoid the marked direction",
        ["돌진 예고 · 주황 경로 옆으로 피하세요"] = "Charge incoming · Move beside the orange path",
        ["와 머야~!! · 손을 뻗기 전에 옆으로"] = "Whoa, what?! · Step aside before the grab",
        ["접시 투척 · 옆으로 이동하세요"] = "Plate throw · Move sideways",
        ["팝콘 세 갈래 · 틈으로 피하세요"] = "Three-way popcorn · Dodge through a gap",
        ["골목 택시 진입 · 왼쪽 빈 차로로"] = "Taxi entering · Take the clear left lane",
        ["교차 차량 · 표시된 위험 구역을 피하세요"] = "Crossing traffic · Avoid the marked area",
        ["배달 오토바이 진입 · 왼쪽 차로가 열려 있습니다"] = "Delivery bike · The left lane is clear",
        ["역주행 킥보드 · 오른쪽으로 피하세요"] = "Oncoming scooter · Dodge right",
        ["열린 맨홀 · 검은 구멍을 피하세요"] = "Open manhole · Avoid the dark hole",
        ["오른쪽 바닥 경고 · 왼쪽으로 피하세요"] = "Danger on the right · Dodge left",
        ["왼쪽 바닥 경고 · 오른쪽으로 피하세요"] = "Danger on the left · Dodge right",
        ["철근 낙하 · 그림자가 없는 왼쪽으로"] = "Falling rebar · Take the unshadowed left side",
        ["TV 낙하 · 그림자를 피하세요"] = "Falling TV · Avoid the shadow",
        ["배식 카트 통과 · 왼쪽 통로는 열려 있습니다"] = "Serving cart · The left aisle is clear",
        ["좌우로 드래그해 회전 · 손을 떼면 정지"] = "Drag sideways to turn · Release to stop"
    };

    // Only bounded chapter notice fragments are translated here, never scene names,
    // fictional storefront brands, save keys, or the notice queue's identifiers.
    private static readonly KeyValuePair<string, string>[] Fragments =
    {
        new("쇼핑 인파 골목", "Busy shopping lane"),
        new("택시·배달 골목", "Taxi/delivery lane"),
        new("B1 식당가", "B1 Food court"), new("5F 영화관", "5F Cinema"),
        new("사람 적음", "Fewer people"), new("차량 많음", "Heavy traffic"),
        new("1회 보호막", "One-hit shield"), new("보호막", "Shield"),
        new("체력", "HP"), new("회복", "Heal"), new("공격", "ATK"), new("코인", "Coins")
    };

    public static string Localize(string text)
    {
        if (string.IsNullOrEmpty(text) || !English) return text;
        if (EnglishText.TryGetValue(text, out var exact)) return exact;
        if (text.IndexOf('\n') >= 0)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = Localize(lines[i]);
            return string.Join("\n", lines);
        }
        // Concurrent warning rows may prefix an order number before the detail.
        foreach (var pair in EnglishText)
            if (text.Contains(pair.Key)) text = text.Replace(pair.Key, pair.Value);
        text = Regex.Replace(text, @"^체력 (\d+)% 회복$", "Restored $1% HP", RegexOptions.CultureInvariant);
        foreach (var pair in Fragments) text = text.Replace(pair.Key, pair.Value);
        text = Regex.Replace(text, @"주문 (\d+)번", "Order $1", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"(\d+)초 후", "In $1s", RegexOptions.CultureInvariant);
        return text;
    }

    public static string CinemaStatus(int seconds, int kills) => English
        ? $"Survive {seconds}s · Kills {kills}" : $"생존 {seconds}초 · 처치 {kills}";
}
